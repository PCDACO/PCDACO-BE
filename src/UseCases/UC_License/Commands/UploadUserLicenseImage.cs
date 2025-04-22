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
            //check if license exists
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == currentUser.User!.Id && !u.IsDeleted,
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
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png"];

        public Validator()
        {
            RuleFor(x => x.LicenseImageFrontUrl)
                .NotNull()
                .WithMessage("Ảnh mặt trước giấy phép không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Kích thước ảnh không được vượt quá 10MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", _allowedExtensions)}"
                );

            RuleFor(x => x.LicenseImageBackUrl)
                .NotNull()
                .WithMessage("Ảnh mặt sau giấy phép không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Kích thước ảnh không được vượt quá 10MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", _allowedExtensions)}"
                );
        }

        private bool ValidateFileSize(Stream file)
        {
            if (file == null)
                return false;
            bool validSize = file.Length <= 10 * 1024 * 1024; // 10MB
            file.Position = 0;
            return validSize;
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

            return fileBytes[..2].SequenceEqual(new byte[] { 0xFF, 0xD8 }) // JPEG and JPG
                || fileBytes[..4].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG
        }
    }
}
