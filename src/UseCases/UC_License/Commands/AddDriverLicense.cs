using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Driver.Commands;

public sealed class AddDriverLicense
{
    public sealed record Command(Guid DriverId, string LicenseNumber, DateTime ExpirationDate)
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
            //check if user is not driver
            if (!currentUser.User!.IsDriver())
                return Result.Error("Bạn không có quyền thực hiện chức năng này");

            //check if driver is exist
            var driver = await context
                .Users.AsNoTracking()
                .Include(u => u.License)
                .FirstOrDefaultAsync(
                    u =>
                        u.Id == request.DriverId
                        && u.Role != null
                        && EF.Functions.ILike(u.Role.Name, "%Driver%")
                        && !u.IsDeleted,
                    cancellationToken
                );

            if (driver is null)
                return Result.Error("Người dùng không tồn tại");

            //check if driver aldready has license
            if (driver.License is not null)
                return Result.Error("Người dùng đã có giấy phép lái xe");

            //check if driver is not current user
            if (driver.Id != currentUser.User!.Id)
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

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
                UserId = request.DriverId,
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
        private readonly string[] allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tiff",
            ".webp",
        };

        public Validator()
        {
            RuleFor(x => x.DriverId)
                .NotEmpty()
                .WithMessage("Phải chọn id của người lái cần tạo giấy phép !");
            RuleFor(x => x.LicenseNumber)
                .NotEmpty()
                .WithMessage("Số giấy phép không được để trống")
                .Matches(@"^\d{12}$")
                .WithMessage("Số giấy phép phải là 12 chữ số");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Ngày hết hạn không được để trống")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("Ngày hết hạn phải lớn hơn hoặc bằng ngày hiện tại");
        }
    }
}
