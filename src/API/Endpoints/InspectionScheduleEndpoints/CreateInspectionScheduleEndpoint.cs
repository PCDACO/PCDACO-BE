using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class CreateInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inspection-schedules", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Create inspection schedule for a car");
    }

    private async Task<IResult> Handle(ISender sender, CreateInspectionScheduleRequest request)
    {
        Result<CreateInspectionSchedule.Response> result = await sender.Send(
            new CreateInspectionSchedule.Command(
                TechnicianId: request.TechnicianId,
                CarId: request.CarId,
                InspectionAddress: request.InspectionAddress,
                InspectionDate: request.InspectionDate
            )
        );
        return result.MapResult();
    }

    private record CreateInspectionScheduleRequest(
        Guid TechnicianId,
        Guid CarId,
        string InspectionAddress,
        DateTimeOffset InspectionDate
    );
}
