using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetModelsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models", Handle)
            .WithSummary("Get all models")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "name")] string? name = "",
        [FromQuery(Name = "manufacturerName")] string? manufacturerName = "",
        [FromQuery(Name = "releaseDate")] DateTimeOffset? releaseDate = null
    )
    {
        Result<OffsetPaginatedResponse<GetAllModels.Response>> result = await sender.Send(
            new GetAllModels.Query(
                pageNumber!.Value,
                pageSize!.Value,
                name!,
                manufacturerName!,
                releaseDate
            )
        );
        return result.MapResult();
    }
}
