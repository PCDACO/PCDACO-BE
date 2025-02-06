using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Amenity.Commands;

public sealed class UpdateAmenity
{
    public record Command(Guid Id, string Name, string Description, Stream? Icon = null)
        : IRequest<Result>;

    internal class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        ICloudinaryServices cloudinaryServices
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            Amenity? updatingAmenity = await context.Amenities.FirstOrDefaultAsync(
                a => a.Id == request.Id,
                cancellationToken
            );
            if (updatingAmenity is null)
                return Result.NotFound("Không tìm thấy tiện nghi");
            // Update the amenity
            updatingAmenity.Name = request.Name;
            updatingAmenity.Description = request.Description;
            updatingAmenity.UpdatedAt = DateTimeOffset.UtcNow;
            if (request.Icon is not null)
            {
                var iconUrl = await cloudinaryServices.UploadAmenityIconAsync(
                    $"Amenity-{updatingAmenity.Id}-IconImage-{Uuid.NewRandom}-UpdateAt-{updatingAmenity.UpdatedAt}",
                    request.Icon,
                    cancellationToken
                );
                updatingAmenity.IconUrl = iconUrl;
            }
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật tiện nghi thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
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
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("tên không được để trống")
                .MaximumLength(50)
                .WithMessage("tên không được quá 50 ký tự");
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("mô tả không được để trống")
                .MaximumLength(500)
                .WithMessage("mô tả không được quá 500 ký tự");
            RuleFor(x => x.Icon)
                .Must((icon) => icon == null || (ValidateFileSize(icon) && ValidateFileType(icon)))
                .WithMessage(
                    (command, icon) =>
                        icon == null ? null
                        : !ValidateFileSize(icon) ? "Biểu tượng không được vượt quá 10MB"
                        : $"Chỉ chấp nhận các định dạng: {string.Join(", ", allowedExtensions)}"
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
