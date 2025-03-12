using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_UserStatistic.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserStatisticEndpoints;

public class GetUserStatisticsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-statistics", Handle)
            .WithSummary("Get current user's statistics")
            .WithDescription(
                "Retrieves various statistics for the current authenticated user including booking counts, earnings, ratings, and inspection schedules"
            )
            .WithTags("User Statistics")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetUserStatistics.Response> result = await sender.Send(
            new GetUserStatistics.Query()
        );
        return result.MapResult();
    }
}
