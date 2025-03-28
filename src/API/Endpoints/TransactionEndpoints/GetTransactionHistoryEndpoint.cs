using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Transaction.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransactionEndpoints;

public class GetTransactionHistoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", Handle)
            .WithSummary("Get transaction history")
            .WithDescription(
                "Get paginated list of user's transactions with offset-based pagination"
            )
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [AsParameters] GetTransactionHistoryRequest request
    )
    {
        var result = await sender.Send(
            new GetTransactionHistory.Query(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.TransactionType,
                request.FromDate,
                request.ToDate
            )
        );

        return result.MapResult();
    }

    public record GetTransactionHistoryRequest(
        [FromQuery(Name = "index")] int PageNumber = 1,
        [FromQuery(Name = "size")] int PageSize = 10,
        [FromQuery(Name = "keyword")] string? SearchTerm = null,
        [FromQuery(Name = "type")] string? TransactionType = null,
        [FromQuery(Name = "from-date")] DateTimeOffset? FromDate = null,
        [FromQuery(Name = "to-date")] DateTimeOffset? ToDate = null
    );
}
