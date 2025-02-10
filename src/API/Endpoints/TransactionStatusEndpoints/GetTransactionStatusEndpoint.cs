using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_TransactionStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;


namespace API.Endpoints.TransactionStatusEndpoints;

public class GetTransactionStatusesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transaction-statuses", Handle)
            .WithSummary("Get Transaction Statuses")
            .WithTags("Transaction Statuses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetTransactionStatuses.Response>> result =
            await sender.Send(new GetTransactionStatuses.Query(
                pageNumber!.Value,
                pageSize!.Value,
                keyword!
            ));
        return result.MapResult();
    }
}