
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_UserRole.Commands;

public class DeleteUserRole
{
    public record Command(Guid Id) : IRequest<Result>;
    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            // Check if user role is exist
            UserRole? deletingUserRole = await context.UserRoles
                .Include(ur => ur.Users)
                .Where(ur => !ur.IsDeleted)
                .Where(ur => ur.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (deletingUserRole is null)
                return Result.NotFound("Không tìm thấy vai trò người dùng");
            if (deletingUserRole.Users.Count != 0)
                return Result.Error("Không thể xóa vai trò người dùng được sử dụng");
            // Delete user role
            deletingUserRole.Delete();
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Xóa vai trò người dùng thành công");
        }
    }
}