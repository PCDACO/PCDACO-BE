using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Withdraw.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.WithdrawalEndpoints;

public class GetAllWithdrawalRequestsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/withdrawals", Handle)
            .WithSummary("Get all withdrawal requests")
            .WithDescription(
                "Admin endpoint to get all withdrawal requests with filtering and pagination"
            )
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [AsParameters] GetAllWithdrawalRequestsRequest request
    )
    {
        var result = await sender.Send(
            new GetAllWithdrawalRequests.Query(
                request.Limit,
                request.LastId,
                request.SearchTerm,
                request.Status,
                request.FromDate,
                request.ToDate
            )
        );

        return result.MapResult();
    }

    public record GetAllWithdrawalRequestsRequest(
        [FromQuery] int Limit = 10,
        [FromQuery] Guid? LastId = null,
        [FromQuery] string? SearchTerm = null,
        [FromQuery] WithdrawRequestStatusEnum? Status = null,
        [FromQuery] DateTimeOffset? FromDate = null,
        [FromQuery] DateTimeOffset? ToDate = null
    );
}
