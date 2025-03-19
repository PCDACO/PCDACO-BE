using System.Text;
using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_License.Commands;

public sealed class UploadUserLicenseImage
{
    public sealed record Command(Stream LicenseImageFrontUrl, Stream LicenseImageBackUrl)
        : IRequest<Result<Response>>;

    public sealed record Response(
        Guid UserId,
        string LicenseImageFrontUrl,
        string LicenseImageBackUrl
    )
    {
        public static Response FromEntity(User user) =>
            new(user.Id, user.LicenseImageFrontUrl, user.LicenseImageBackUrl);
    };

    public sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        ICloudinaryServices cloudinaryServices
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            //check if user is not driver or owner
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Error("Bạn không có quyền thực hiện chức năng này");

            //check if license exists
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == currentUser.User.Id && !u.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            if (string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            // Upload new images
            var frontImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"License-User-{user.Id}-FrontImage",
                request.LicenseImageFrontUrl,
                cancellationToken
            );
            var backImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"License-User-{user.Id}-BackImage",
                request.LicenseImageBackUrl,
                cancellationToken
            );

            // Update license images
            user.LicenseImageFrontUrl = frontImageUrl;
            user.LicenseImageBackUrl = backImageUrl;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.LicenseImageUploadedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(user),
                "Cập nhật ảnh giấy phép lái xe thành công"
            );
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
            ".svg",
            ".heic",
            ".heif",
        };

        public Validator()
        {
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

            return fileBytes[..2].SequenceEqual(new byte[] { 0xFF, 0xD8 })
                || // JPEG and JPG
                fileBytes[..4].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
                || // PNG
                fileBytes[..3].SequenceEqual(new byte[] { 0x47, 0x49, 0x46 })
                || // GIF
                fileBytes[..2].SequenceEqual(new byte[] { 0x42, 0x4D })
                || // BMP
                fileBytes[..4].SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 })
                || // WebP
                fileBytes[..4].SequenceEqual(new byte[] { 0x49, 0x49, 0x2A, 0x00 })
                || // TIFF (Little-endian)
                fileBytes[..4].SequenceEqual(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })
                || // TIFF (Big-endian)
                Encoding.UTF8.GetString(fileBytes).Contains("<svg")
                || // SVG
                (
                    fileBytes.Length >= 12
                    && fileBytes[4] == 0x66
                    && fileBytes[5] == 0x74
                    && fileBytes[6] == 0x79
                    && fileBytes[7] == 0x70
                    && (
                        (
                            fileBytes[8] == 0x68
                            && fileBytes[9] == 0x65
                            && fileBytes[10] == 0x69
                            && fileBytes[11] == 0x63
                        )
                        || // HEIC
                        (
                            fileBytes[8] == 0x68
                            && fileBytes[9] == 0x65
                            && fileBytes[10] == 0x69
                            && fileBytes[11] == 0x66
                        )
                    )
                ); // HEIF
        }
    }
}
