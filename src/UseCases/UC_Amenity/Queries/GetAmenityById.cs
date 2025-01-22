
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Amenity.Queries;

public sealed class GetAmenityById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        string Description,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(Amenity amenity)
            => new(
                amenity.Id,
                amenity.Name,
                amenity.Description,
                GetTimestampFromUuid.Execute(amenity.Id)
            );
    };

    public class Handler(
        IAppDBContext contet
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Amenity? gettingAmenity = await contet.Amenities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (gettingAmenity is null)
                return Result.NotFound("Không tìm thấy tiện nghi");
            return Result.Success(Response.FromEntity(gettingAmenity));
        }
    }
}