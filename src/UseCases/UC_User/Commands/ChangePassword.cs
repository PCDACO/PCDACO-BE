using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public static class ChangePassword
{
    public record Command(Guid UserId, string OldPassword, string NewPassword)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId)
    {
        public static Response FromEntity(User user) => new(user.Id);
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                x => x.Id == request.UserId && !x.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            if (_currentUser.User!.Id != user.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            string hashedOldPassword = request.OldPassword.HashString();

            if (user.Password != hashedOldPassword)
            {
                return Result.Error(ResponseMessages.OldPasswordIsInvalid);
            }

            if (user.Password == request.NewPassword.HashString())
            {
                return Result.Error(ResponseMessages.NewPasswordIsSameAsOld);
            }

            user.Password = request.NewPassword.HashString();
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), ResponseMessages.Updated);
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Mật khẩu cũ không được để trống");
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Mật khẩu mới không được để trống")
                .MinimumLength(6)
                .WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự");
        }
    }
}
