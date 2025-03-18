using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Withdraw.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.WithdrawalEndpoints;

public class ConfirmWithdrawalRequestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/withdrawals/{id}/confirm", Handle)
            .WithSummary("Confirm a withdrawal request")
            .WithDescription(
                "Admin endpoint to confirm a withdrawal request with transaction proof"
            )
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromRoute] Guid id,
        [FromForm] ConfirmWithdrawalRequestRequest request
    )
    {
        var result = await sender.Send(
            new ConfirmWithdrawalRequest.Command(
                id,
                request.TransactionProof.OpenReadStream(),
                request.AdminNote
            )
        );

        return result.MapResult();
    }

    public record ConfirmWithdrawalRequestRequest(
        IFormFile TransactionProof,
        string? AdminNote = null
    );
}
