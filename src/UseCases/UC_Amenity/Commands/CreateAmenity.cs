using System.IO;
using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Amenity.Commands;

public sealed class CreateAmenity
{
    public record Command(string Name, string Description, Stream Icon)
        : IRequest<Result<Response>>;

    public record Response(Guid Id)
    {
        public static Response FromEntity(Amenity amenity)
        {
            return new Response(amenity.Id);
        }
    };

    public class Handler(
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
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");

            Amenity amenity = new()
            {
                Name = request.Name,
                Description = request.Description,
                IconUrl = "",
            };
            var iconUrl = await cloudinaryServices.UploadAmenityIconAsync(
                $"Amenity-{amenity.Id}-IconImage-{Uuid.NewRandom()}",
                request.Icon,
                cancellationToken
            );
            amenity.IconUrl = iconUrl;
            await context.Amenities.AddAsync(amenity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Created(Response.FromEntity(amenity));
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
                .WithMessage("tên không vượt quá 50 kí tự");
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("mô tả không được để trống")
                .MaximumLength(500)
                .WithMessage("mô tả không vượt quá 500 kí tự");
            RuleFor(x => x.Icon)
                .NotNull()
                .WithMessage("Biểu tượng không được để trống")
                .Must(ValidateFileSize)
                .WithMessage("Biểu tượng không được vượt quá 10MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", allowedExtensions)}"
                );
            ;
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
