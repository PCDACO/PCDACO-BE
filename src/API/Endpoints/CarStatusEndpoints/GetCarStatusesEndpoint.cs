using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_CarStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarStatusEndpoints;

public class GetCarStatusesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car-statuses", Handle)
            .WithSummary("Get car statuses")
            .WithTags("Car Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = "")
    {
        Result<OffsetPaginatedResponse<GetCarStatuses.Response>> result =
            await sender.Send(new GetCarStatuses.Query(pageNumber, pageSize, keyword!));
        return result.MapResult();
    }
}