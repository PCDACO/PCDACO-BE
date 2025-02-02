
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_CompensationStatus.Queries;

public class GetCompensationStatuses
{
    public record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string Keyword = ""
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(CompensationStatus entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
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
                return Result.Forbidden("Bạn không có quyển truy cập");
            // Get query
            IQueryable<CompensationStatus> query = context.CompensationStatuses
                .Where(e => EF.Functions.ILike(e.Name, $"%{request.Keyword}%"))
                .Where(x => !x.IsDeleted);
            // Get total count of tables
            int count = await query.CountAsync(cancellationToken);
            // Check if next page have any datas
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Get data
            List<Response> data = await query
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .Select(cs => Response.FromEntity(cs))
                .ToListAsync(cancellationToken);
            // Return result
            return Result.Success(new OffsetPaginatedResponse<Response>(
                    data,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ));
        }
    }
}