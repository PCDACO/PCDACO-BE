using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_License.Commands;

public sealed class AddUserLicense
{
    public sealed record Command(string LicenseNumber, DateTimeOffset ExpirationDate)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(License license) => new(license.Id);
    };

    public sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
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
            //check if user is not driver or owner

            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            //check if user is exist

            var user = await context
                .Users.AsNoTracking()
                .Include(u => u.License)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && u.Role != null && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            //check if user aldready has license

            if (user.License is not null)
                return Result.Error("Người dùng đã có giấy phép lái xe");

            //check if user is not owner role or driver role

            if (user.Role!.Name.ToLower() != "owner" && user.Role.Name.ToLower() != "driver")
                return Result.Error("Chỉ chủ xe hoặc tài xế mới có thể thêm giấy phép lái xe");

            //check if license number is already exist

            var licenses = await context
                .Licenses.AsNoTracking()
                .Include(l => l.EncryptionKey)
                .ToListAsync(cancellationToken);

            var licenseChecks = await Task.WhenAll(
                licenses.Select(async l =>
                {
                    string decryptedKey = keyManagementService.DecryptKey(
                        l.EncryptionKey.EncryptedKey,
                        encryptionSettings.Key
                    );

                    string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                        l.EncryptedLicenseNumber,
                        decryptedKey,
                        l.EncryptionKey.IV
                    );

                    return decryptedLicenseNumber == request.LicenseNumber;
                })
            );

            if (licenseChecks.Any(exists => exists))
            {
                return Result.Error("Số giấy phép lái xe đã tồn tại");
            }

            // Encrypt license number

            (string key, string iv) = await keyManagementService.GenerateKeyAsync();

            string encryptedLicenseNumber = await aesEncryptionService.Encrypt(
                request.LicenseNumber,
                key,
                iv
            );

            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);

            EncryptionKey newEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };

            context.EncryptionKeys.Add(newEncryptionKey);

            // Create new license

            var license = new License
            {
                UserId = currentUser.User!.Id,

                EncryptedLicenseNumber = encryptedLicenseNumber,

                EncryptionKeyId = newEncryptionKey.Id,

                ExpiryDate = request.ExpirationDate.ToString("yyyy-MM-dd"),

                IsApprove = null,
            };

            await context.Licenses.AddAsync(license, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(license), "Thêm giấy phép lái xe thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseNumber)
                .NotEmpty()
                .WithMessage("Số giấy phép không được để trống")
                .Matches(@"^\d{12}$")
                .WithMessage("Số giấy phép phải là 12 chữ số");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Ngày hết hạn không được để trống")
                .Must(date => date.Date >= DateTimeOffset.UtcNow.Date)
                .WithMessage("Thời điểm hết hạn phải lớn hơn hoặc bằng thời điểm hiện tại");
        }
    }
}
