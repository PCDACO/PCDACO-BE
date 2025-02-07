using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CreateFeedbackEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{id:guid}/feedback", Handle)
            .WithSummary("Create a new feedback when booking is completed")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        CreateFeedbackRequest request
    )
    {
        Result result = await sender.Send(
            new CreateFeedBack.Command(id, request.Rating, request.Comment)
        );

        return result.MapResult();
    }

    private sealed record CreateFeedbackRequest(int Rating, string Comment);
}
