using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Booking.Commands;

namespace API.Endpoints.BookingEndpoints;

public class ProcessBookingPaymentByTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/payment/{token}", Handle)
            .WithSummary("Process booking payment using email token")
            .WithTags("Bookings")
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        [FromRoute] string token,
        [FromServices] ISender sender,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new ProcessBookingPaymentByToken.Command(token),
            cancellationToken
        );
        return result.MapResult();
    }
}
