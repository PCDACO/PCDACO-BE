
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_UserRole.Queries;

public class GetUserRoles
{
    public record Query(
        int PageNumber,
        int PageSize,
        string Keyword
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;
    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(UserRole entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id));
    };
    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền truy cập");
            // Query user roles
            IQueryable<UserRole> query = context
                .UserRoles.AsNoTracking()
                .Where(ur => EF.Functions.ILike(ur.Name, $"%{request.Keyword}%"));
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            // Check if next page have any datas
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Get user roles
            IEnumerable<Response> userRoles = await query
                .OrderByDescending(ur => ur.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ur => Response.FromEntity(ur))
                .ToListAsync(cancellationToken);
            // Return result
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    userRoles,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext), "Lấy danh sách vai trò người dùng thành công");
        }
    }
}