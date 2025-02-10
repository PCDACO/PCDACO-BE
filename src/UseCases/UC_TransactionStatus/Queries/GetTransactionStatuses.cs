

using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

using UseCases.DTOs;

using UseCases.Utils;

namespace UseCases.UC_TransactionStatus.Queries;

public class GetTransactionStatuses
{
    public record Query(int PageNumber, int PageSize, string Keyword) : IRequest<Result<OffsetPaginatedResponse<Response>>>;
    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(TransactionStatus entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
    }
    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền truy cập");
            IQueryable<TransactionStatus> query = context.TransactionStatuses
                .Where(e => EF.Functions.ILike(e.Name, $"%{request.Keyword}%"))
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.Id);
            int count = await query.CountAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            List<Response> data = await query
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .Select(e => Response.FromEntity(e))
                .ToListAsync(cancellationToken);
            return Result.Success(new OffsetPaginatedResponse<Response>(data, count, request.PageNumber, request.PageSize, hasNext));
        }
    }

}