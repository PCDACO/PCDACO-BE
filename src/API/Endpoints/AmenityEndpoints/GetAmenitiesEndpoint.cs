using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

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
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = "")
    {
        Result<OffsetPaginatedResponse<GetAmenities.Response>> result = await sender.Send(
            new GetAmenities.Query(
                pageNumber!.Value,
                pageSize!.Value,
                keyword!
            )
        );
        return result.MapResult();
    }
}