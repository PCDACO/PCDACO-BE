using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_Feedback.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FeedbackEndpoints;

public class GetAllFeedbackForCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feedbacks/current-user", Handle)
            .WithSummary("Get all feedbacks for the current user")
            .WithDescription("Retrieves all feedbacks sent to the current user (owner or driver)")
            .WithTags("Feedbacks")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllFeedbackForCurrentUser.Response>> result =
            await sender.Send(
                new GetAllFeedbackForCurrentUser.Query(
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Keyword: keyword ?? string.Empty
                )
            );

        return result.MapResult();
    }
}
