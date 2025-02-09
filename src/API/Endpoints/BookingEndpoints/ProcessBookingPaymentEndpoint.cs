using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Booking.Commands;

namespace API.Endpoints.BookingEndpoints;

public class ProcessBookingPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{id:guid}/payment", Handle)
            .WithSummary("Create a payment for a booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new ProcessBookingPayment.Command(id), cancellationToken);
        return result.MapResult();
    }
}
