using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_InspectionSchedule.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetAllInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Get all inspection schedules filtered by technician, month and year");
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery] Guid? technicianId,
        [FromQuery] MonthEnum? month = null,
        [FromQuery] int? year = null
    )
    {
        Result<IEnumerable<GetAllInspectionSchedules.Response>> result = await sender.Send(
            new GetAllInspectionSchedules.Query(
                TechnicianId: technicianId,
                Month: month,
                Year: year
            )
        );
        return result.MapResult();
    }
}
