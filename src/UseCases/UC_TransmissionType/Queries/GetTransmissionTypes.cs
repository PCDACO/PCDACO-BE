
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_TransmissionType.Queries;

public class GetTransmissionTypes
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
        public static Response FromEntity(TransmissionType entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Query transmission types
            IQueryable<TransmissionType> query = context
                .TransmissionTypes.AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Name, $"%{request.Keyword}%"))
                .Where(t => !t.IsDeleted);
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Get transmission types
            IEnumerable<Response> transmissionTypes = await query
                .OrderByDescending(t => t.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => Response.FromEntity(t))
                .ToListAsync(cancellationToken);
            return Result.Success(OffsetPaginatedResponse<Response>.Map(
                transmissionTypes,
                count,
                request.PageNumber,
                request.PageSize,
                hasNext),
                "Lấy danh sách trẻn động");
        }
    }
}
