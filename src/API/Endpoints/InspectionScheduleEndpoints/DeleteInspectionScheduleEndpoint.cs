using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class DeleteInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/inspection-schedules/{id:guid}", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Delete an inspection schedule");
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteInspectionSchedule.Command(id));
        return result.MapResult();
    }
}
