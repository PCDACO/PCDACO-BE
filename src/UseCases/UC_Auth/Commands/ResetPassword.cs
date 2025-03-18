using Ardalis.Result;
using Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Auth.Commands;

public class ResetPassword
{
    public record Command(string NewPassword) : IRequest<Result>;

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Find user in database
            var user = await _context.Users.FirstOrDefaultAsync(
                x => x.Id == _currentUser.User!.Id && !x.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            // Hash the new password
            string hashedNewPassword = request.NewPassword.HashString();

            // Check if the new password is the same as the current one
            if (user.Password == hashedNewPassword)
                return Result.Error("Đây là mật khẩu cũ của bạn");

            // Update the password
            user.Password = hashedNewPassword;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Cập nhật mật khẩu thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Mật khẩu mới không được để trống")
                .MinimumLength(6)
                .WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự");
        }
    }
}
