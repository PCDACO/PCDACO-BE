using API.Utils;

using Ardalis.Result;

using Carter;

using Infrastructure.Idempotency;

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
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CreateBookingRequest request)
    {
        Result<CreateBooking.Response> result = await sender.Send(
            new CreateBooking.CreateBookingCommand(
                request.UserId,
                request.CarId,
                request.StatusId,
                request.StartTime,
                request.EndTime
            )
        );

        return result.MapResult();
    }

    private sealed record CreateBookingRequest(
        Guid UserId,
        Guid CarId,
        Guid StatusId,
        DateTime StartTime,
        DateTime EndTime
    );
}
