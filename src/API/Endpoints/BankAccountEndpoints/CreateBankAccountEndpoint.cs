using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using UseCases.UC_BankAccount.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BankAccountEndpoints;

public class CreateBankAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bank-accounts", Handle)
            .WithSummary("Create a new bank account for the current user")
            .WithTags("Bank Accounts")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CreateBankAccountRequest request)
    {
        Result<CreateBankAccount.Response> result = await sender.Send(
            new CreateBankAccount.Command(
                BankInfoId: request.BankInfoId,
                AccountNumber: request.AccountNumber,
                AccountName: request.AccountName,
                IsPrimary: request.IsPrimary
            )
        );
        return result.MapResult();
    }

    private record CreateBankAccountRequest(
        Guid BankInfoId,
        string AccountNumber,
        string AccountName,
        bool IsPrimary = false
    );
}
