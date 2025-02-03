using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_BookingStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingStatusEndpoints;

public class UpdateBookingStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/booking-statuses/{id:guid}", Handle)
            .WithSummary("Update a booking status")
            .WithTags("Booking Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, UpdateBookingStatusRequest request)
    {
        Result result = await sender.Send(new UpdateBookingStatus.Command(
            id,
            request.Name
        ));
        return result.MapResult();
    }
    private record UpdateBookingStatusRequest(
        string Name
    );
}