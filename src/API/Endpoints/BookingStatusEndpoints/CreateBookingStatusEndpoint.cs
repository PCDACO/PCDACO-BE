using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Newtonsoft.Json;

using UseCases.UC_BookingStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingStatusEndpoints;

public class CreateBookingStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/booking-statuses", Handle)
            .WithSummary("Create a new booking status")
            .WithTags("Booking Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CreateBookingStatusRequest request)
    {
        Result<CreateBookingStatus.Response> result = await sender.Send(new CreateBookingStatus.Command(request.Name));
        return result.MapResult();
    }
    private record CreateBookingStatusRequest(
        [JsonProperty("name")] string Name);
}