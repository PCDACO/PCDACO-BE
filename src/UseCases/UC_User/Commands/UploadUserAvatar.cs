using System.Text;
using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Commands;

public sealed class UploadUserAvatar
{
    public sealed record Command(Guid UserId, Stream Avatar) : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId, string AvatarUrl)
    {
        public static Response FromEntity(User user) => new(user.Id, user.AvatarUrl);
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
            // Get user
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == request.UserId && !u.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.NotFound("Người dùng không tồn tại");

            // Check if user is updating their own profile
            if (currentUser.User!.Id != user.Id)
                return Result.Forbidden("Không có quyền cập nhật ảnh đại diện của người dùng khác");

            // Upload avatar to Cloudinary
            string avatarUrl = await cloudinaryServices.UploadUserImageAsync(
                $"User-{user.Id}-Avatar",
                request.Avatar,
                cancellationToken
            );

            // Update user avatar URL
            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), "Cập nhật ảnh đại diện thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png"];

        public Validator()
        {
            RuleFor(c => c.UserId).NotEmpty().WithMessage("Id người dùng không được để trống");
            RuleFor(c => c.Avatar)
                .NotNull()
                .WithMessage("Ảnh đại diện không được để trống")
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
