
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_ContractStatus.Queries;

public class GetContractStatusById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;
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
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? result = await context.ContractStatuses
                .Where(cs => cs.Id == request.Id)
                .Where(cs => !cs.IsDeleted)
                .Select(cs => Response.FromEntity(cs))
                .FirstOrDefaultAsync(cancellationToken)!;
            if (result is null)
                return Result.NotFound("Không tìm thấy trạng thái hợp đồng");
            return Result.Success(result, "Lấy trạng thái hợp đồng thành công");
        }
    }
}