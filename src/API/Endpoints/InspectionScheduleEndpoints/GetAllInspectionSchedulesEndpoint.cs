using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
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
            .WithSummary("Get all inspection schedules filtered by technician, month and year")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve all inspection schedules with optional filtering.

                    Features:
                    - Filter by technician ID
                    - Filter by month
                    - Filter by year
                    - Returns all matching inspection schedules

                    Notes:
                    - Only consultants and technicians can access this endpoint
                    - For technicians, they can only see schedules assigned to them
                    - For consultants, they can see all schedules
                    - When no filters are provided, returns all schedules
                    - When only year is provided, returns all schedules for that year
                    - When month and year are provided, returns schedules for that specific month
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns list of inspection schedules",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiArray
                                        {
                                            new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                                ),
                                                ["technicianId"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa7"
                                                ),
                                                ["technicianName"] = new OpenApiString(
                                                    "Nguyễn Văn Kỹ Thuật"
                                                ),
                                                ["carId"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa8"
                                                ),
                                                ["carOwnerId"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa9"
                                                ),
                                                ["carOwnerName"] = new OpenApiString(
                                                    "Trần Văn Chủ"
                                                ),
                                                ["statusName"] = new OpenApiString("Pending"),
                                                ["note"] = new OpenApiString("Kiểm định định kỳ"),
                                                ["inspectionAddress"] = new OpenApiString(
                                                    "123 Đường Lê Lợi, Quận 1, TP.HCM"
                                                ),
                                                ["inspectionDate"] = new OpenApiString(
                                                    "2023-12-31T14:30:00Z"
                                                ),
                                                ["createdAt"] = new OpenApiString(
                                                    "2023-12-25T09:15:00Z"
                                                ),
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a consultant or technician",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện thao tác này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid parameters",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Tham số không hợp lệ"),
                                    },
                                },
                            },
                        },
                    },
                }
            );
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
