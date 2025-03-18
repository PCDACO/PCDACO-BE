using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using UseCases.UC_Withdraw.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.WithdrawalEndpoints;

public class CreateWithdrawalRequestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/withdrawal", Handle)
            .WithSummary("Create a withdrawal request")
            .WithTags("Transactions")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CreateWithdrawalRequestRequest request
    )
    {
        Result<CreateWithdrawalRequest.Response> result = await sender.Send(
            new CreateWithdrawalRequest.Command(request.BankAccountId, request.Amount)
        );

        return result.MapResult();
    }

    private sealed record CreateWithdrawalRequestRequest(Guid BankAccountId, decimal Amount);
}
