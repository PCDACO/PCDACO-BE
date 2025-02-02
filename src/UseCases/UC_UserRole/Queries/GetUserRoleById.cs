
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_UserRole.Queries;

public class GetUserRoleById
{
    public record Query(
        Guid Id
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(UserRole entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Văn có quyền truy cập");
            // Get user role
            Response? userRole = await context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.Id == request.Id)
                .Where(ur => !ur.IsDeleted)
                .Select(ur => Response.FromEntity(ur))
                .FirstOrDefaultAsync(cancellationToken);
            if (userRole is null)
                return Result.NotFound("Không tìm thấy vai trò người dùng");
            // Return result
            return Result.Success(userRole, "Lấy thông tin vai trò người dùng thành công");
        }
    }
}