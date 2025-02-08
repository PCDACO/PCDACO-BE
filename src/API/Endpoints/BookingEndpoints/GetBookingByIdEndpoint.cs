using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Booking.Queries;

namespace API.Endpoints.BookingEndpoints;

public class GetBookingByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id}", Handle)
            .WithSummary("Get booking details by ID")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        ISender sender,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new GetBookingById.Query(id), cancellationToken);
        return result.MapResult();
    }
}
