
using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_GPSDevice.Queries;

public class GetGPSDevices
{
    public record Query(
        int PageNumber,
        int PageSize,
        string Keyword
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
     Guid Id,
     string Name,
     string Status,
     DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(GPSDevice device)
            => new(
                device.Id,
                device.Name,
                device.Status.ToString(),
               GetTimestampFromUuid.Execute(device.Id)
            );
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<GPSDevice> gpsQuery = context.GPSDevices
                .AsNoTracking()
                .Include(gps => gps.Status)
                .Where(gps => !gps.IsDeleted)
                .Where(gps => EF.Functions.ILike(gps.Name, $"%{request.Keyword}%"))
                .OrderByDescending(gps => gps.Id);
            IEnumerable<Response> result = await gpsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(gps => Response.FromEntity(gps))
                .ToListAsync(cancellationToken);
            int count = await gpsQuery.CountAsync(cancellationToken);
            bool hasNext = await gpsQuery
                .Skip(request.PageNumber * request.PageSize)
                .AnyAsync(cancellationToken);
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    result,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ), ResponseMessages.Fetched
            );
        }
    }
}