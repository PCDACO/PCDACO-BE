using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_BankAccount.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BankAccountEndpoints;

public class UpdateBankAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bank-accounts/{id:guid}", Handle)
            .WithSummary("Update a bank account for the current user")
            .WithTags("Bank Accounts")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateBankAccountRequest request)
    {
        Result<UpdateBankAccount.Response> result = await sender.Send(
            new UpdateBankAccount.Command(
                Id: id,
                BankInfoId: request.BankInfoId,
                AccountNumber: request.AccountNumber,
                AccountName: request.AccountName,
                IsPrimary: request.IsPrimary
            )
        );
        return result.MapResult();
    }

    private record UpdateBankAccountRequest(
        Guid BankInfoId,
        string AccountNumber,
        string AccountName,
        bool IsPrimary = false
    );
}
