using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_ContractStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractStatusEndpoints;

public class GetContractStatusesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/contract-statuses", Handle)
            .WithSummary("Get contract statuses")
            .WithTags("Contract Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetContractStatuses.Response>> result = await sender.Send(new GetContractStatuses.Query(pageNumber!.Value, pageSize!.Value, keyword!));
        return result.MapResult();
    }
}