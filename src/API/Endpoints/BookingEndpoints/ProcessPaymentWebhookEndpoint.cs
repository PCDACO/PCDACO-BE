using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class ProcessPaymentWebhookEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhook", Handle)
            .WithSummary("Handle payment webhook, called by PayOS to notify payment status")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromBody] WebhookType webhookType,
        CancellationToken cancellationToken = default
    )
    {
        Result<CreateBooking.Response> result = await sender.Send(
            new ProcessPaymentWebhook.Command(webhookType),
            cancellationToken
        );

        return result.MapResult();
    }
}
