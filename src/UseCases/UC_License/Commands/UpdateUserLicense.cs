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
    public sealed record Command(string LicenseNumber, DateTimeOffset ExpirationDate)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId, string LicenseNumber, DateTimeOffset ExpirationDate)
    {
        public static Response FromEntity(User user) =>
            new(user.Id, user.EncryptedLicenseNumber, user.LicenseExpiryDate!.Value);
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
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == currentUser.User.Id && !u.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            if (string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            //check if license number is already exist
            var users = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .ToListAsync(cancellationToken);

            var licenseChecks = await Task.WhenAll(
                users.Select(async u =>
                {
                    if (u.Id == user.Id) // Skip current user
                        return false;

                    string decryptedKey = keyManagementService.DecryptKey(
                        u.EncryptionKey.EncryptedKey,
                        encryptionSettings.Key
                    );

                    string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                        u.EncryptedLicenseNumber,
                        decryptedKey,
                        u.EncryptionKey.IV
                    );

                    return decryptedLicenseNumber == request.LicenseNumber;
                })
            );

            if (licenseChecks.Any(exists => exists))
            {
                return Result.Error("Số giấy phép lái xe đã tồn tại");
            }

            // Encrypt new license number
            string decryptedKey = keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            string encryptedLicenseNumber = await aesEncryptionService.Encrypt(
                request.LicenseNumber,
                decryptedKey,
                user.EncryptionKey.IV
            );

            // Update license
            user.EncryptedLicenseNumber = encryptedLicenseNumber;
            user.LicenseExpiryDate = request.ExpirationDate;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.LicenseIsApproved = null; // Reset approval status
            user.LicenseRejectReason = null; // Clear reject reason
            user.LicenseApprovedAt = null; // Clear accepted date

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(user),
                "Cập nhật giấy phép lái xe thành công"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseNumber)
                .NotEmpty()
                .WithMessage("Số giấy phép lái xe không được để trống")
                .Length(12)
                .WithMessage("Số giấy phép lái xe phải có 12 ký tự");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Ngày hết hạn không được để trống")
                .Must(date => date >= DateTimeOffset.UtcNow)
                .WithMessage("Thời điểm hết hạn phải lớn hơn hoặc bằng thời điểm hiện tại");
        }
    }
}
