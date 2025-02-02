using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_CompensationStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CompensationStatusEndpoints;

public class GetCompensationStatusesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/compensation-statuses", Handle)
            .WithSummary("Get compensation statuses")
            .WithTags("Compensation Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetCompensationStatuses.Response>> result =
            await sender.Send(new GetCompensationStatuses.Query(pageNumber!.Value, pageSize!.Value, keyword!));
        return result.MapResult();
    }
}