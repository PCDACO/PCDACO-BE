using System.ComponentModel;
using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
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
            .WithSummary(
                "Get all inspection schedules by technician name or car owner name or inspection address, filter by inspection date and sort by options"
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery]
        [Description("Search by technician name or car owner name or inspection address")]
            string? keyword = null,
        [FromQuery] DateTimeOffset? inspectionDate = null,
        [FromQuery] string? sortOrder = "desc"
    )
    {
        Result<OffsetPaginatedResponse<GetAllInspectionSchedules.Response>> result =
            await sender.Send(
                new GetAllInspectionSchedules.Query(
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Keyword: keyword,
                    InspectionDate: inspectionDate,
                    SortOrder: sortOrder ?? "desc"
                )
            );
        return result.MapResult();
    }
}
