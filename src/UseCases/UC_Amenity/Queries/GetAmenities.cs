using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Amenity.Queries;

public sealed class GetAmenities
{
    public record Query(int PageNumber = 1, int PageSize = 10, string keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(Guid Id, string Name, string Description, string IconUrl, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(Amenity amenity) =>
            new(
                amenity.Id,
                amenity.Name,
                amenity.Description,
                amenity.IconUrl,
                GetTimestampFromUuid.Execute(amenity.Id)
            );
    };

    public class Handler(IAppDBContext context)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Query amenities
            IQueryable<Amenity> query = context
                .Amenities.AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Name, $"%{request.keyword}%"))
            .Where(a => !a.IsDeleted);
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            // Get amenities
            IEnumerable<Response> amenities = await query
                .OrderByDescending(a => a.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => Response.FromEntity(a))
                .ToListAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
            // Return result
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    amenities,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
