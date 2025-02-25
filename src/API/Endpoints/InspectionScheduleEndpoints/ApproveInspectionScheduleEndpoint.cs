using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class ApproveInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inspection-schedules/{id:guid}/approve", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Approve an inspection schedule");
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        ApproveInspectionScheduleRequest request
    )
    {
        Result<ApproveInspectionSchedule.Response> result = await sender.Send(
            new ApproveInspectionSchedule.Command(
                Id: id,
                Note: request.Note,
                IsApproved: request.IsApproved
            )
        );
        return result.MapResult();
    }

    private record ApproveInspectionScheduleRequest(string Note, bool IsApproved);
}
