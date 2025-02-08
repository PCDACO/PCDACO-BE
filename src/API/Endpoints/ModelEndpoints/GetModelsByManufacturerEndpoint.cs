using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetModelsByManufacturerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manufacturers/{id:guid}/models", Handle)
            .WithSummary("Get all models of a manufacturer")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromRoute] Guid id,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? name = ""
    )
    {
        Result<OffsetPaginatedResponse<GetModelsByManufacturer.Response>> result =
            await sender.Send(
                new GetModelsByManufacturer.Query(
                    ManufacturerId: id!,
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Name: name!
                )
            );
        return result.MapResult();
    }
}
