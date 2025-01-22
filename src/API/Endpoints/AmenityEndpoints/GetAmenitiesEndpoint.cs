using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.DTOs;
using UseCases.UC_Amenity.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AmenityEndpoints;

public class GetAmenitiesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/amenities", Handle)
            .WithSummary("Get amenities")
            .WithTags("Amenities")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [AsParameters] GetAmenitiesRequest request)
    {
        Result<OffsetPaginatedResponse<GetAmenities.Response>> result = await sender.Send(
            new GetAmenities.Query(
                request.pageNumber!.Value,
                request.pageSize!.Value,
                request.keyword!
            )
        );
        return result.MapResult();
    }

    private record GetAmenitiesRequest(
        int? pageNumber = 1,
        int? pageSize = 10,
        string? keyword = ""
    );
}