using System.Text;
using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Manufacturer.Commands;

public sealed class UploadManufacturerLogo
{
    public sealed record Command(Guid ManufacturerId, Stream LogoImage)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid ManufacturerId, string LogoUrl)
    {
        public static Response FromEntity(Manufacturer manufacturer) =>
            new(manufacturer.Id, manufacturer.LogoUrl);
    }

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
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get manufacturer
            var manufacturer = await context.Manufacturers.FirstOrDefaultAsync(
                m => m.Id == request.ManufacturerId && !m.IsDeleted,
                cancellationToken
            );

            if (manufacturer is null)
                return Result.NotFound("Không tìm thấy hãng xe");

            // Upload logo to cloudinary
            string logoUrl = await cloudinaryServices.UploadManufacturerLogoAsync(
                $"Manufacturer-{manufacturer.Id}-Logo-{Guid.NewGuid()}",
                request.LogoImage,
                cancellationToken
            );

            // Update manufacturer with new logo URL
            manufacturer.LogoUrl = logoUrl;
            manufacturer.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(manufacturer),
                "Cập nhật logo hãng xe thành công"
            );
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        private readonly string[] _allowedExtensions =
        [
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
        ];

        public Validator()
        {
            RuleFor(x => x.ManufacturerId).NotEmpty().WithMessage("ID hãng xe không được để trống");

            RuleFor(x => x.LogoImage)
                .NotNull()
                .WithMessage("Ảnh logo không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Kích thước ảnh logo không được lớn hơn 10MB")
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
