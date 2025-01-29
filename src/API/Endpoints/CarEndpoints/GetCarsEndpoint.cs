using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using UseCases.DTOs;
using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars", Handle)
            .WithSummary("Get cars")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "latitude")] decimal? latitude,
        [FromQuery(Name = "longtitude")] decimal? longtitude,
        [FromQuery(Name = "radius")] decimal? radius,
        [FromQuery(Name = "manufacturer")] Guid? manufacturer,
        [FromQuery(Name = "lastCarId")] Guid? lastCarId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "limit")] int? limit = 10
    )
    {
        Result<OffsetPaginatedResponse<GetCars.Response>> result = await sender.Send(new GetCars.Query(
            latitude,
            longtitude,
            radius,
            manufacturer,
            amenities,
            fuel,
            transmission,
            lastCarId,
            limit!.Value
        ));
        return result.MapResult();
    }

    private record GetCarsRequest(
        [FromQuery(Name = "latitude")] decimal? Latitude,
        [FromQuery(Name = "longtitude")] decimal? Longtitude,
        [FromQuery(Name = "radius")] decimal? Radius,
        [FromQuery(Name = "manufacturer")] Guid? Manufacturer,
        [FromQuery(Name = "lastCarId")] Guid? LastCarId,
        [FromQuery(Name = "amenities")] Guid[]? Amenities,
        [FromQuery(Name = "limit")] int? Limit = 10
    );
}