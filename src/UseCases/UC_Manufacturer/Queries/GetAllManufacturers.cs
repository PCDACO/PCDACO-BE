using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Manufacturer.Queries;

public class GetAllManufacturers
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10, string keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(Manufacturer manufacturer) =>
            new(manufacturer.Id, manufacturer.Name, GetTimestampFromUuid.Execute(manufacturer.Id));
    };

    public sealed class Handler(IAppDBContext context)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Query manufacturers
            IQueryable<Manufacturer> query = context
                .Manufacturers.AsNoTracking()
                .Where(m => EF.Functions.ILike(m.Name, $"%{request.keyword}%"));
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            // Get manufacturers
            IEnumerable<Response> manufacturers = await query
                .OrderByDescending(m => m.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => Response.FromEntity(m))
                .ToListAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Return result
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    manufacturers,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                "Lấy danh sách hãng xe thành công"
            );
        }
    }
}