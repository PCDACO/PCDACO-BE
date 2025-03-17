using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class StartBookingTripEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/start-trip", Handle)
            .WithSummary("Driver start a trip for a booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        StartBookingTripRequest request
    )
    {
        Result result = await sender.Send(
            new StartBookingTrip.Command(id, request.Latitude, request.Longitude)
        );
        return result.MapResult();
    }

    private sealed record StartBookingTripRequest(decimal Latitude, decimal Longitude);
}
