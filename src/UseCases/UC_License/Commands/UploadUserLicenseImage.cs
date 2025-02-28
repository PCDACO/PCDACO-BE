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
    public sealed record Command(
        Guid LicenseId,
        Stream LicenseImageFrontUrl,
        Stream LicenseImageBackUrl
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id, string LicenseImageFrontUrl, string LicenseImageBackUrl)
    {
        public static Response FromEntity(License license)
        {
            return new(license.Id, license.LicenseImageFrontUrl, license.LicenseImageBackUrl);
        }
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
            var license = await context.Licenses.FirstOrDefaultAsync(
                l => l.Id == request.LicenseId && !l.IsDeleted,
                cancellationToken
            );

            if (license is null)
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            //check if current user is owner of the license
            if (license.UserId != currentUser.User!.Id)
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            // Upload new images
            var frontImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"License-{request.LicenseId}-FrontImage",
                request.LicenseImageFrontUrl,
                cancellationToken
            );
            var backImageUrl = await cloudinaryServices.UploadDriverLicenseImageAsync(
                $"License-{request.LicenseId}-BackImage",
                request.LicenseImageBackUrl,
                cancellationToken
            );

            // Update license images
            license.LicenseImageFrontUrl = frontImageUrl;
            license.LicenseImageBackUrl = backImageUrl;
            license.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(license),
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
        };

        public Validator()
        {
            RuleFor(x => x.LicenseId)
                .NotEmpty()
                .WithMessage("Phải chọn giấy phép lái xe cần cập nhật !");

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
