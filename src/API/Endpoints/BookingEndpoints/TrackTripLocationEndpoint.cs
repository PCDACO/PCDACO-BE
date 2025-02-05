using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class TrackTripLocationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{id:guid}/track", Handle)
            .WithSummary("Track trip location")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        TrackTripLocationRequest request
    )
    {
        Result result = await sender.Send(
            new TrackTripLocation.Command(id, request.Latitude, request.Longitude)
        );

        return result.MapResult();
    }

    private sealed record TrackTripLocationRequest(decimal Latitude, decimal Longitude);
}
