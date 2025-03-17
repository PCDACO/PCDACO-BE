using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_BankAccount.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BankAccountEndpoints;

public class DeleteBankAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/bank-accounts/{id:guid}", Handle)
            .WithSummary("Delete a bank account for the current user")
            .WithTags("Bank Accounts")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        Result result = await sender.Send(new DeleteBankAccount.Command(id), cancellationToken);
        return result.MapResult();
    }
}
