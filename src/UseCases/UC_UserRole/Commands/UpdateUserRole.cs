
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_UserRole.Commands;

public class UpdateUserRole
{
    public record Command(
        Guid Id,
        string Name
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");
            // Check if user role is exist
            UserRole? updatingUserRole = await context.UserRoles
                .Where(ur => !ur.IsDeleted)
                .Where(ur => ur.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (updatingUserRole is null)
                return Result.NotFound("Không tìm thấy vai trò người dùng");
            // Update user role
            updatingUserRole.Name = request.Name;
            updatingUserRole.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Cập nhật vai trò người dùng thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên vai trò");
        }
    }
}