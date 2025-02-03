using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class ApproveBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/approve", Handle)
            .WithSummary("Owner approve or reject a booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        ApproveBookingRequest request
    )
    {
        Result result = await sender.Send(new ApproveBooking.Command(id, request.IsApproved));
        return result.MapResult();
    }

    private sealed record ApproveBookingRequest(bool IsApproved);
}
