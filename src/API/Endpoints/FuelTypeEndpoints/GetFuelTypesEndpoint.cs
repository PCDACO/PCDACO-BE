using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_FuelType.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FuelTypeEndpoints;

public class GetFuelTypesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/fuel-types", Handle)
            .WithSummary("Get fuel types")
            .WithTags("Fuel Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetFuelTypes.Response>> result = await sender.Send(new GetFuelTypes.Query(
            pageNumber!.Value,
            pageSize!.Value,
            keyword!
        ));
        return result.MapResult();
    }
}