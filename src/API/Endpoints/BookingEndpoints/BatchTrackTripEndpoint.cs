using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using static UseCases.UC_Booking.Commands.BatchTrackTrip;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class BatchTrackTripEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{id:guid}/track/batch", Handle)
            .WithSummary("Batch track trip location")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        BatchTrackTripRequest request
    )
    {
        Result result = await sender.Send(new Command(id, request.LocationPoints));

        return result.MapResult();
    }

    private sealed record BatchTrackTripRequest(IEnumerable<LocationPoint> LocationPoints);
}
