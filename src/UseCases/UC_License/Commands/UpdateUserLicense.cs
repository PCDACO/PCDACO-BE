using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_License.Commands;

public sealed class UpdateUserLicense
{
    public sealed record Command(
        Guid LicenseId,
        string LicenseNumber,
        DateTimeOffset ExpirationDate
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(License license) => new(license.Id);
    }

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
            // Check if user is driver or owner
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            // Get license
            var license = await context.Licenses.FirstOrDefaultAsync(
                l => l.Id == request.LicenseId && !l.IsDeleted,
                cancellationToken
            );

            if (license == null)
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            // Verify ownership
            if (license.UserId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            // Encrypt new license number
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedLicenseNumber = await aesEncryptionService.Encrypt(
                request.LicenseNumber,
                key,
                iv
            );

            // Create new encryption key
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            var newEncryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
            context.EncryptionKeys.Add(newEncryptionKey);

            // Update license
            license.EncryptedLicenseNumber = encryptedLicenseNumber;
            license.EncryptionKeyId = newEncryptionKey.Id;
            license.ExpiryDate = request.ExpirationDate;
            license.UpdatedAt = DateTimeOffset.UtcNow;
            license.IsApprove = null; // Reset approval status
            license.RejectReason = null; // Clear reject reason
            license.ApprovedAt = null; // Clear accepted date

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(license),
                "Cập nhật giấy phép lái xe thành công"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseId).NotEmpty().WithMessage("Chưa cung cấp ID giấy phép lái xe");

            RuleFor(x => x.LicenseNumber)
                .NotEmpty()
                .WithMessage("Số giấy phép lái xe không được để trống")
                .Length(12)
                .WithMessage("Số giấy phép lái xe phải có 12 ký tự");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Ngày hết hạn không được để trống")
                .Must(date => date.Date >= DateTimeOffset.UtcNow.Date)
                .WithMessage("Thời điểm hết hạn phải lớn hơn hoặc bằng thời điểm hiện tại");
        }
    }
}
