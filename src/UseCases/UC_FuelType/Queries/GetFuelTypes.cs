
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_FuelType.Queries;

public class GetFuelTypes
{
    public record Query(int PageNumber, int PageSize, string Keyword) :
        IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(FuelType entity)
            => new(entity.Id, entity.Name, GetTimestampFromUuid.Execute(entity.Id));
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Query fuel types
            IQueryable<FuelType> query = context
                .FuelTypes
                .AsNoTracking()
                .Where(ft => EF.Functions.ILike(ft.Name, $"%{request.Keyword}%"));
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Get fuel types
            IEnumerable<Response> fuelTypes = await query
                .OrderByDescending(ft => ft.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ft => Response.FromEntity(ft))
                .ToListAsync(cancellationToken);
            // Return result
            return Result.Success(OffsetPaginatedResponse<Response>
                .Map(fuelTypes, count, request.PageNumber, request.PageSize, hasNext),
                    "Lấy danh sách loại nhân liệu thành công"
                );
        }
    }
}