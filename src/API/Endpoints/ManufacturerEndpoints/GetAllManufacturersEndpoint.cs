using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_Manufacturer.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class GetAllManufacturersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manufacturers", Handle)
            .WithSummary("Get all manufacturers")
            .WithTags("Manufacturers")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllManufacturers.Response>> result = await sender.Send(
            new GetAllManufacturers.Query(
                pageNumber!.Value,
                pageSize!.Value,
                keyword!
            )
        );
        return result.MapResult();
    }
}
