using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;


using UseCases.UC_InspectionSchedule.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInProgressInspectionScheduleForCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/in-progress", Handle)
            .WithSummary("Get current in-progress inspection schedules of current user(technician)")
            .WithTags("Inspection Schedules")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        Result<GetInProgressInspectionScheduleForCurrentUser.Response> result = await sender.Send(new GetInProgressInspectionScheduleForCurrentUser.Query(), cancellationToken);
        return result.MapResult();
    }
}