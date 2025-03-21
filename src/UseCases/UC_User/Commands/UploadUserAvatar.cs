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
        public Validator()
        {
            RuleFor(c => c.UserId).NotEmpty().WithMessage("Id người dùng không được để trống");
            RuleFor(c => c.Avatar).NotNull().WithMessage("Ảnh đại diện không được để trống");
        }
    }
}
