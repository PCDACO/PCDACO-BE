using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class InProgressInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inspection-schedules/{id:guid}/inprogress", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Mark an inspection schedule as in progress");
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<InProgressInspectionSchedule.Response> result = await sender.Send(
            new InProgressInspectionSchedule.Command(id)
        );
        return result.MapResult();
    }
}
