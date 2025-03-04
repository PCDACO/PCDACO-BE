using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_Feedback.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FeedbackEndpoints;

public class GetAllFeedbacksByBookingIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id:guid}/feedbacks", Handle)
            .WithSummary("Get all feedbacks for a specific booking")
            .WithTags("Feedbacks")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllFeedbacksByBookingId.Response>> result =
            await sender.Send(
                new GetAllFeedbacksByBookingId.Query(
                    BookingId: id,
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Keyword: keyword ?? string.Empty
                )
            );

        return result.MapResult();
    }
}
