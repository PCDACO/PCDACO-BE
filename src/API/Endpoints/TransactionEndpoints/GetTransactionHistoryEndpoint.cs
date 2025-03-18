using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
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
                "Get paginated list of user's transactions including bookings, withdrawals, etc."
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
                request.Limit,
                request.LastId,
                request.SearchTerm,
                request.TransactionType,
                request.FromDate,
                request.ToDate
            )
        );

        return result.MapResult();
    }

    public record GetTransactionHistoryRequest(
        [FromQuery] int Limit = 10,
        [FromQuery] Guid? LastId = null,
        [FromQuery] string? SearchTerm = null,
        [FromQuery] string? TransactionType = null,
        [FromQuery] DateTimeOffset? FromDate = null,
        [FromQuery] DateTimeOffset? ToDate = null
    );
}
