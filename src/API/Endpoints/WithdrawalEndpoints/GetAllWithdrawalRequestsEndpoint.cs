using API.Utils;
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
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                request.FromDate,
                request.ToDate
            )
        );

        return result.MapResult();
    }

    public record GetAllWithdrawalRequestsRequest(
        [FromQuery(Name = "index")] int PageNumber = 1,
        [FromQuery(Name = "size")] int PageSize = 10,
        [FromQuery(Name = "keyword")] string? SearchTerm = "",
        [FromQuery(Name = "status")] WithdrawRequestStatusEnum? Status = null,
        [FromQuery(Name = "from-date")] DateTimeOffset? FromDate = null,
        [FromQuery(Name = "to-date")] DateTimeOffset? ToDate = null
    );
}
