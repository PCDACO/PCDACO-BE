using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_BookingStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingStatusEndpoints;

public class GetBookingStatusByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/booking-statuses/{id:guid}", Handle)
        .WithSummary("Get booking status by id")
        .WithTags("Booking Statuses")
        .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetBookingStatusById.Response> result = await sender.Send(new GetBookingStatusById.Query(id));
        return result.MapResult();
    }
}