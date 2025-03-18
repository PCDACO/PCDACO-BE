using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_BankAccount.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BankAccountEndpoints;

public class GetBankAccountByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bank-accounts/{id:guid}", Handle)
            .WithSummary("Get a bank account by ID for the current user")
            .WithDescription(
                "Returns details of a specific bank account if it belongs to the current user"
            )
            .WithTags("Bank Accounts")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        Result<GetBankAccountById.Response> result = await sender.Send(
            new GetBankAccountById.Query(id),
            cancellationToken
        );
        return result.MapResult();
    }
}
