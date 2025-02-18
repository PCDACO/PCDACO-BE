using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public sealed class CreateStaff
{
    public record Command(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string RoleName
    ) : IRequest<Result<Response>>;

    public record Response(Guid Id);

    public sealed class Handler(
        IAppDBContext context,
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
            // Check if role name is valid
            if (!new[] { "consultant", "technician" }.Contains(request.RoleName.ToLower()))
                return Result.Error(ResponseMessages.MustBeConsultantOrTechnician);

            // Check if user already exists
            User? existingUser = await context.Users.FirstOrDefaultAsync(
                x =>
                    EF.Functions.ILike(x.Email, request.Email)
                    || EF.Functions.ILike(x.Phone, request.Phone),
                cancellationToken
            );

            if (existingUser is not null)
            {
                return EF.Functions.ILike(existingUser.Email, request.Email)
                    ? Result.Error(ResponseMessages.EmailAddressIsExisted)
                    : Result.Error(ResponseMessages.PhoneNumberIsExisted);
            }

            // Get role
            UserRole? role = await context.UserRoles.FirstOrDefaultAsync(
                r => EF.Functions.ILike(r.Name, request.RoleName),
                cancellationToken
            );

            if (role is null)
                return Result.Error(ResponseMessages.RoleNotFound);

            // Generate encryption key
            var (key, iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedPhone = await aesEncryptionService.Encrypt(request.Phone, key, iv);
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);

            // Create encryption key
            EncryptionKey encryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            await context.EncryptionKeys.AddAsync(encryptionKey, cancellationToken);

            // Create user
            User user = new()
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password.HashString(),
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                Phone = encryptedPhone,
                RoleId = role.Id,
                EncryptionKeyId = encryptionKey.Id,
            };

            await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(user.Id), ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên không được để trống")
                .MinimumLength(2)
                .WithMessage("Tên phải có ít nhất 2 ký tự")
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
                .LessThan(DateTimeOffset.UtcNow)
                .WithMessage("Ngày sinh không hợp lệ");

            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống");

            RuleFor(x => x.RoleName)
                .NotEmpty()
                .WithMessage("Vai trò không được để trống")
                .Must(x => new[] { "consultant", "technician" }.Contains(x.ToLower()))
                .WithMessage("Vai trò phải là consultant hoặc technician");
        }
    }
}
