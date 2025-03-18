using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Withdraw.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.WithdrawalEndpoints;

public class GenerateWithdrawalQRCodeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/withdrawals/{id}/qr", Handle)
            .WithSummary("Generate QR code for withdrawal request")
            .WithDescription(
                "Admin endpoint to generate VietQR code for processing a withdrawal request"
            )
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GenerateWithdrawalQRCode.Query(id));
        return result.MapResult();
    }
}
