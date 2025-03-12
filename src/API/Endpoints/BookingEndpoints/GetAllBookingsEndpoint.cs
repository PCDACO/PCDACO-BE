using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Booking.Queries;

namespace API.Endpoints.BookingEndpoints;

public class GetAllBookingsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings", Handle)
            .WithSummary("Get all bookings")
            .WithDescription(
                """
                Retrieves paginated bookings based on user role:
                - Drivers: Can view all their bookings
                - Owners: Can view all bookings for their cars
                """
            )
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "lastId")] Guid? lastId,
        [FromQuery(Name = "search")] string? searchTerm,
        [FromQuery(Name = "status")] string? status,
        [FromQuery(Name = "isPaid")] bool? isPaid,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new GetAllBookings.Query(limit ?? 10, lastId, searchTerm, status, isPaid),
            cancellationToken
        );

        return result.MapResult();
    }
}