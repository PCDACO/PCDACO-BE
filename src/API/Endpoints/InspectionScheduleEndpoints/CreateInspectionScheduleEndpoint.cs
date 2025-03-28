using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
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
            .WithSummary("Create inspection schedule for a car")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new inspection schedule for a car.

                    Inspection Schedule Rules:
                    - Only consultants can create inspection schedules
                    - The car must exist and be in 'Pending' status
                    - The technician must exist and have the technician role
                    - A car can only have one active schedule at a time
                    - A technician can't have expired schedules with the same car
                    - The technician can't have overlapping appointments (within 1 hour)
                    - The technician can't have approved schedules after the requested time

                    Process:
                    1. Validates consultant permissions
                    2. Validates car and technician eligibility
                    3. Checks for scheduling conflicts
                    4. Creates the inspection schedule
                    5. Returns inspection schedule ID
                    """,

                    Responses =
                    {
                        ["201"] = new()
                        {
                            Description = "Created - Inspection schedule successfully created",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["id"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Tạo mới thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Thời điểm kiểm định phải lớn hơn hoặc bằng thời điểm hiện tại"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a consultant",
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
                        ["404"] = new()
                        {
                            Description = "Not Found - Car or technician not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy xe hoặc kiểm định viên"
                                        ),
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description =
                                "Conflict - Car already has an active inspection schedule",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Xe đã có lịch kiểm định đang hoạt động"
                                        ),
                                    },
                                },
                            },
                        },
                        ["422"] = new()
                        {
                            Description =
                                "Unprocessable Entity - Scheduling conflict with technician",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Kiểm định viên đã có lịch trong vòng 1 giờ của thời điểm này"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, CreateInspectionScheduleRequest request)
    {
        Result<CreateInspectionSchedule.Response> result = await sender.Send(
            new CreateInspectionSchedule.Command(
                TechnicianId: request.TechnicianId,
                CarId: request.CarId,
                InspectionAddress: request.InspectionAddress,
                InspectionDate: request.InspectionDate,
                ReportId: request.ReportId,
                IsIncident: request.IsIncident
            )
        );
        return result.MapResult();
    }

    private record CreateInspectionScheduleRequest(
        Guid TechnicianId,
        Guid CarId,
        string InspectionAddress,
        DateTimeOffset InspectionDate,
        Guid? ReportId = null,
        bool IsIncident = false
    );
}
