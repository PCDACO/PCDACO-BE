using System.Text;

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
    public record Command(string Name, string Description, Stream[] Icon)
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
            List<Task<string>> tasks = [];
            string[] iconUrl = [];
            if (request.Icon.Length > 0)
            {
                foreach (var icon in request.Icon)
                {
                    tasks.Add(cloudinaryServices.UploadAmenityIconAsync(
                $"Amenity-{amenity.Id}-IconImage-{Uuid.NewRandom()}",
                icon,
                cancellationToken));
                }
                iconUrl = await Task.WhenAll(tasks);
            }
            if (iconUrl.Length > 0)
                amenity.IconUrl = iconUrl[0];

            await context.Amenities.AddAsync(amenity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Created(Response.FromEntity(amenity));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        private readonly string[] _allowedExtensions =
        ["svg"];

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
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", _allowedExtensions)}"
                );
            ;
        }

        private bool ValidateFileSize(Stream[] file)
        {
            return file?.Length <= 10 * 1024 * 1024; // 10MB
        }

        private bool ValidateFileType(Stream file)
            => IsSvg(file);

        private static bool IsSvg(Stream fileStream)
        {
            if (fileStream == null)
                return false;

            // Preserve the original position if the stream supports seeking
            long originalPosition = 0;
            if (fileStream.CanSeek)
            {
                originalPosition = fileStream.Position;
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            string content;
            using (var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                content = reader.ReadToEnd();
            }

            // Reset the stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Seek(originalPosition, SeekOrigin.Begin);
            }


            // Check if the content contains the <svg tag (case-insensitive)
            return content.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
