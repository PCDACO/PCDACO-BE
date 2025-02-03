
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_ContractStatus.Queries;

public class GetContractStatuses
{
    public record Query(
        int PageNumber,
        int PageSize,
        string Keyword
          ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
        )
    {
        public static Response FromEntity(ContractStatus contractStatus)
            => new(contractStatus.Id, contractStatus.Name, GetTimestampFromUuid.Execute(contractStatus.Id));
    };
    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Query contract statuses
            IQueryable<ContractStatus> query = context
                .ContractStatuses.AsNoTracking()
                .Where(cs => !cs.IsDeleted)
                .Where(cs => EF.Functions.ILike(cs.Name, $"%{request.Keyword}%"));
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Get contract statuses
            IEnumerable<Response> contractStatuses = await query
                .OrderByDescending(cs => cs.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(cs => Response.FromEntity(cs))
                .ToListAsync(cancellationToken);
            return Result.Success(OffsetPaginatedResponse<Response>.Map(
                contractStatuses,
                count,
                request.PageNumber,
                request.PageSize,
                hasNext
                ), "Lấy danh sách trạng thái hợp đồng thành công");
        }
    }
}