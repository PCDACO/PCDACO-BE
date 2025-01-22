using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

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
    private async Task<IResult> Handle(ISender sender, [AsParameters] GetCarsRequest request)
    {
        Result<OffsetPaginatedResponse<GetCars.Response>> result = await sender.Send(new GetCars.Query(
            request.Latitude,
            request.Longtitude,
            request.Radius,
            request.Manufacturer,
            request.Amenities,
            request.LastCarId,
            request.Limit!.Value
        ));
        return result.MapResult();
    }

    private record GetCarsRequest(
        [JsonProperty("latitude")] decimal? Latitude,
        [JsonProperty("longtitude")] decimal? Longtitude,
        [JsonProperty("radius")] decimal? Radius,
        [JsonProperty("manufacturer")] Guid? Manufacturer,
        [JsonProperty("lastCarId")] Guid? LastCarId,
        [JsonProperty("amenities")] Guid[]? Amenities,
        [JsonProperty("limit")] int? Limit = 10
    );
}