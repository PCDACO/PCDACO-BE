using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_InspectionSchedule.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInspectionScheduleDetailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/{id:guid}", Handle)
            .WithSummary("Get inspection schedules detail by id")
            .WithTags("Inspection Schedules")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, CancellationToken cancellationToken)
    {
        Result<GetInspectionScheduleDetail.Response> result = await sender.Send(new GetInspectionScheduleDetail.Query(id), cancellationToken);
        return result.MapResult();
    }
}
