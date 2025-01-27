using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.DTOs;
using UseCases.UC_BookingStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingStatusEndpoints;

public class GetBookingStatusesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/booking-statuses", Handle)
            .WithSummary("Get booking statuses")
            .WithTags("Booking Statuses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, [AsParameters] GetBookingStatusesRequest request)
    {
        Result<OffsetPaginatedResponse<GetBookingStatuses.Response>> result =
            await sender.Send(new GetBookingStatuses.Query(request.PageSize!.Value, request.PageNumber!.Value));
        return result.MapResult();
    }
    private record GetBookingStatusesRequest(int? PageSize = 10, int? PageNumber = 1);
}