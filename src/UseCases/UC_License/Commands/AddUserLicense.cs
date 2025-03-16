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

    public sealed record Response(Guid UserId, string LicenseNumber, DateTimeOffset ExpirationDate)
    {
        public static Response FromEntity(User user) =>
            new(user.Id, user.EncryptedLicenseNumber, user.LicenseExpiryDate!.Value);
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
                .Users.Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            //check if user aldready has license
            if (!string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.Error("Người dùng đã có giấy phép lái xe");

            //check if license number is already exist
            var licenses = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
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
            string decryptedKey = keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            string encryptedLicenseNumber = await aesEncryptionService.Encrypt(
                request.LicenseNumber,
                decryptedKey,
                user.EncryptionKey.IV
            );

            // Add license info to user

            user.EncryptedLicenseNumber = encryptedLicenseNumber;
            user.LicenseExpiryDate = request.ExpirationDate;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.LicenseIsApproved = null;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), "Thêm giấy phép lái xe thành công");
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
                .Must(date => date >= DateTimeOffset.UtcNow)
                .WithMessage("Thời điểm hết hạn phải lớn hơn hoặc bằng thời điểm hiện tại");
        }
    }
}
