using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;

namespace API.Endpoints.BookingEndpoints;

public class MarkBookingReadyForPickupEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/ready-for-pickup", Handle)
            .WithSummary("Mark a booking as ready for pickup")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new MarkBookingReadyForPickup.Command(id));
        return result.MapResult();
    }
}
