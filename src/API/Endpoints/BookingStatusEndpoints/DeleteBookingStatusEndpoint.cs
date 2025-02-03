using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_BookingStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingStatusEndpoints;

public class DeleteBookingStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/booking-status/{id:guid}", Handle)
            .WithSummary("Delete a booking status")
            .WithTags("Booking Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteBookingStatus.Command(id));
        return result.MapResult();
    }
}