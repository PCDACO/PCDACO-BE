using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class UpdateInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/inspection-schedules/{id:guid}", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Update an inspection schedule");
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateInspectionScheduleRequest request
    )
    {
        Result<UpdateInspectionSchedule.Response> result = await sender.Send(
            new UpdateInspectionSchedule.Command(
                Id: id,
                TechnicianId: request.TechnicianId,
                InspectionAddress: request.InspectionAddress,
                InspectionDate: request.InspectionDate
            )
        );
        return result.MapResult();
    }

    private record UpdateInspectionScheduleRequest(
        Guid TechnicianId,
        string InspectionAddress,
        DateTimeOffset InspectionDate
    );
}
