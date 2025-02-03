using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Driver.Commands;

public sealed class UpdateDriverLicense
{
    public sealed record Command(
        string LicenseNumber,
        Stream LicenseImageFrontUrl,
        Stream LicenseImageBackUrl,
        string Fullname,
        DateTime ExpirationDate
    ) : IRequest<Result>;

    public sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        ICloudinaryServices cloudinaryServices
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            //check if user is not driver
            if (!currentUser.User!.IsDriver())
                return Result.Error("Bạn không có quyền thực hiện chức năng này");

            //check if driver's information is exist
            var driver = await context
                .Drivers.Include(d => d.User)
                .Include(d => d.EncryptionKey)
                .FirstOrDefaultAsync(d => d.UserId == currentUser.User!.Id, cancellationToken);

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

            // Upload new images
            var frontImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"DriverHasUserId-{currentUser.User!.Id}-License-{encryptedLicenseNumber}-FrontImage",
                request.LicenseImageFrontUrl,
                cancellationToken
            );
            var backImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"DriverHasUserId-{currentUser.User!.Id}-License-{encryptedLicenseNumber}-BackImage",
                request.LicenseImageBackUrl,
                cancellationToken
            );

            if (driver is null)
            {
                // Create new driver
                driver = new Driver
                {
                    UserId = currentUser.User!.Id,
                    EncryptedLicenseNumber = encryptedLicenseNumber,
                    EncryptionKeyId = newEncryptionKey.Id,
                    LicenseImageFrontUrl = frontImageUrl,
                    LicenseImageBackUrl = backImageUrl,
                    Fullname = request.Fullname,
                    ExpiryDate = request.ExpirationDate.ToString("yyyy-MM-dd"),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsApprove = null,
                };
                await context.Drivers.AddAsync(driver, cancellationToken);
            }
            else
            {
                driver.EncryptedLicenseNumber = encryptedLicenseNumber;
                driver.LicenseImageFrontUrl = frontImageUrl;
                driver.LicenseImageBackUrl = backImageUrl;
                driver.Fullname = request.Fullname;
                driver.ExpiryDate = request.ExpirationDate.ToString("yyyy-MM-dd");
                driver.UpdatedAt = DateTimeOffset.UtcNow;
                driver.IsApprove = null;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Cập nhật giấy phép lái xe thành công");
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
            RuleFor(x => x.LicenseNumber)
                .NotEmpty()
                .WithMessage("Số giấy phép không được để trống")
                .Matches(@"^\d{12}$")
                .WithMessage("Số giấy phép phải là 12 chữ số");

            RuleFor(x => x.LicenseImageFrontUrl)
                .NotNull()
                .WithMessage("Ảnh mặt trước giấy phép không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Kích thước ảnh không được vượt quá 10MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", allowedExtensions)}"
                );

            RuleFor(x => x.LicenseImageBackUrl)
                .NotNull()
                .WithMessage("Ảnh mặt sau giấy phép không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Kích thước ảnh không được vượt quá 10MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", allowedExtensions)}"
                );

            RuleFor(x => x.Fullname)
                .NotEmpty()
                .WithMessage("Họ tên không được để trống")
                .Length(2, 50)
                .WithMessage("Họ tên phải từ 2 đến 50 ký tự")
                .Matches(@"^[\p{L}\s]+$")
                .WithMessage("Họ tên chỉ được chứa chữ cái và khoảng trắng");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty()
                .WithMessage("Ngày hết hạn không được để trống")
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Ngày hết hạn phải lớn hơn ngày hiện tại");
        }

        private bool ValidateFileSize(Stream file)
        {
            return file?.Length <= 10 * 1024 * 1024; // 10MB
        }

        private bool ValidateFileType(Stream file)
        {
            if (file == null)
                return false;

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
                file.Position = 0; // Reset stream position
            }

            return IsValidImageFile(fileBytes);
        }

        private bool IsValidImageFile(byte[] fileBytes)
        {
            if (fileBytes.Length < 4)
                return false;

            // Check file signatures
            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8)
                return true; // JPEG
            if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50)
                return true; // PNG
            if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49)
                return true; // GIF
            if (fileBytes[0] == 0x42 && fileBytes[1] == 0x4D)
                return true; // BMP

            return false;
        }
    }
}
