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
        app.MapGet("/api/inspection-schedules/technician/today", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Get today's inspection schedules for current technician");
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery] string? sortOrder = "desc"
    )
    {
        Result<OffsetPaginatedResponse<GetInDateScheduleForCurrentTechnician.Response>> result =
            await sender.Send(
                new GetInDateScheduleForCurrentTechnician.Query(
                    pageNumber!.Value,
                    pageSize!.Value,
                    sortOrder
                )
            );
        return result.MapResult();
    }
}
