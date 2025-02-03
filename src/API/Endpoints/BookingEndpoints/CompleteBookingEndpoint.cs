using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CompleteBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/complete", Handle)
            .WithSummary("Driver complete a booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new CompleteBooking.Command(id));
        return result.MapResult();
    }
}
