using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetAllModelsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models", Handle)
            .WithSummary("Get all models with filtering")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetModels.Response>> result = await sender.Send(
            new GetModels.Query(
                PageNumber: pageNumber!.Value,
                PageSize: pageSize!.Value,
                Keyword: keyword!
            )
        );
        return result.MapResult();
    }
}
