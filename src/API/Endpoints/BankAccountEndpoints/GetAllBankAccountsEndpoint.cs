using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_BankAccount.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BankAccountEndpoints;

public class GetAllBankAccountsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bank-accounts", Handle)
            .WithSummary("Get all bank accounts for the current user")
            .WithDescription(
                "Returns a paginated list of all bank accounts owned by the current user"
            )
            .WithTags("Bank Accounts")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = "",
        CancellationToken cancellationToken = default
    )
    {
        Result<OffsetPaginatedResponse<GetAllBankAccounts.Response>> result = await sender.Send(
            new GetAllBankAccounts.Query(
                PageNumber: pageNumber,
                PageSize: pageSize,
                Keyword: keyword!
            ),
            cancellationToken
        );

        return result.MapResult();
    }
}
