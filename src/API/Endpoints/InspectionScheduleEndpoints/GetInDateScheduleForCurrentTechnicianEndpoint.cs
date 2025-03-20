using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInDateScheduleForCurrentTechnicianEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/technician", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Get inspection schedules for current technician by inspection date");
    }

    private async Task<IResult> Handle(ISender sender, DateTimeOffset? inspectionDate = null)
    {
        Result<GetInDateScheduleForCurrentTechnician.Response> result = await sender.Send(
            new GetInDateScheduleForCurrentTechnician.Query(InspectionDate: inspectionDate)
        );
        return result.MapResult();
    }
}
