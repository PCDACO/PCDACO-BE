using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class ConfirmCarReturnEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/return", Handle)
            .WithSummary("Owner confirm car return belong to a booking")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new ConfirmCarReturn.Command(id));
        return result.MapResult();
    }
}
