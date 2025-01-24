using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CreateBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings", Handle)
            .WithSummary("Create a new booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CreateBookingRequest request)
    {
        Result<CreateBooking.Response> result = await sender.Send(
            new CreateBooking.CreateBookingCommand(
                request.UserId,
                request.CarId,
                request.StartTime,
                request.EndTime
            )
        );

        return result.MapResult();
    }

    private sealed record CreateBookingRequest(
        Guid UserId,
        Guid CarId,
        DateTime StartTime,
        DateTime EndTime
    );
}
