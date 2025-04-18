using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public class SignUp
{
    public record Command(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string RoleName = "Driver"
    ) : IRequest<Result<Response>>;

    public record Response(string AccessToken, string RefreshToken);

    public sealed class Handler(
        IAppDBContext context,
        TokenService tokenService,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            List<User> users = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .ToListAsync(cancellationToken);

            foreach (var u in users)
            {
                string decryptedKey = keyManagementService.DecryptKey(
                    u.EncryptionKey.EncryptedKey,
                    encryptionSettings.Key
                );

                string decryptedPhone = await aesEncryptionService.Decrypt(
                    u.Phone,
                    decryptedKey,
                    u.EncryptionKey.IV
                );

                if (u.Email == request.Email)
                {
                    return Result.Error("Email đã tồn tại");
                }

                if (decryptedPhone == request.Phone)
                {
                    return Result.Error("Số điện thoại đã tồn tại");
                }
            }

            UserRole? checkingUserRole = await context.UserRoles.FirstOrDefaultAsync(
                ur => ur.Name.ToLower() == request.RoleName.ToLower(),
                cancellationToken
            );
            if (checkingUserRole is null)
                return Result.Error("Không thể đăng ký tài khoản với vai trò này");

            if (checkingUserRole.Name.Equals("admin", StringComparison.CurrentCultureIgnoreCase))
                return Result.Error("Không thể đăng ký tài khoản với vai trò này");

            string refreshToken = tokenService.GenerateRefreshToken();
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedPhone = await aesEncryptionService.Encrypt(request.Phone, key, iv);
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            EncryptionKey encryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            await context.EncryptionKeys.AddAsync(encryptionKey, cancellationToken);
            User user = new()
            {
                EncryptionKeyId = encryptionKey.Id,
                Name = request.Name,
                Email = request.Email,
                Password = request.Password.HashString(),
                Address = request.Address,
                RoleId = checkingUserRole!.Id,
                DateOfBirth = request.DateOfBirth,
                Phone = encryptedPhone,
            };
            user.RefreshTokens.Add(
                new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTimeOffset.UtcNow.AddHours(24),
                }
            );

            UserStatistic userStatistic = new() { UserId = user.Id };

            await context.Users.AddAsync(user, cancellationToken);
            await context.UserStatistics.AddAsync(userStatistic, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            string accessToken = tokenService.GenerateAccessToken(user);
            return Result.Success(new Response(accessToken, refreshToken), "Đăng ký thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên không được để trống")
                .MinimumLength(3)
                .WithMessage("Tên phải có ít nhất 3 ký tự")
                .MaximumLength(50)
                .WithMessage("Tên không được quá 50 ký tự");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email không được để trống")
                .EmailAddress()
                .WithMessage("Email không hợp lệ");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6)
                .WithMessage("Mật khẩu phải có ít nhất 6 ký tự");

            RuleFor(x => x.Address).NotEmpty().WithMessage("Địa chỉ không được để trống");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("Ngày sinh không được để trống")
                .Must(dob => dob.Date < DateTimeOffset.UtcNow.Date)
                .WithMessage("Ngày sinh phải nhỏ hơn thời điểm hiện tại");

            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống");
        }
    }
}
