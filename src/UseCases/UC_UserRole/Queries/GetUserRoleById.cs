
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
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
        IAppDBContext context
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? userRole = await context.UserRoles
                .Where(ur => ur.Id == request.Id)
                .Where(ur => !ur.IsDeleted)
                .Select(ur => Response.FromEntity(ur))
                .FirstOrDefaultAsync(cancellationToken);
            if (userRole is null)
                return Result.NotFound("Không tìm thấy vai trò người dùng");
            return Result.Success(userRole, "Lấy thông tin vai trò người dùng thành công");
        }
    }
}