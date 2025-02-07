using System.Text;

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
        private readonly string[] _allowedExtensions = ["svg"];
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
                        : $"Chỉ chấp nhận các định dạng: {string.Join(", ", _allowedExtensions)}"
                );
        }

        private bool ValidateFileSize(Stream file)
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
