using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
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
            .WithSummary("Update an inspection schedule")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Update an existing inspection schedule with new details.

                    Features:
                    - Update the assigned technician
                    - Change inspection address
                    - Reschedule inspection date/time

                    Process:
                    1. Validates the schedule exists and is in pending status
                    2. Verifies the new technician exists and has the technician role
                    3. Checks for scheduling conflicts with the technician's other appointments
                    4. Updates the schedule with new information

                    Notes:
                    - Only consultant users can update inspection schedules
                    - Only schedules in pending status can be updated
                    - The inspection date must be in the future
                    - Cannot schedule within 1 hour of another inspection for the same technician
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Inspection schedule updated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["id"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Scheduling conflict with technician",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể tạo lịch kiểm định có thời gian kiểm định cách nhau ít hơn 1 giờ so với lịch kiểm định khác"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad Request - Schedule not in pending status or has conflicts",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Chỉ có thể cập nhật lịch kiểm định đang chờ duyệt"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors or scheduling conflicts",
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
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString(
                                                "Id lịch kiểm định không được để trống"
                                            ),
                                            new OpenApiString(
                                                "Id kiểm định viên không được để trống"
                                            ),
                                            new OpenApiString(
                                                "Địa chỉ kiểm định không được để trống"
                                            ),
                                            new OpenApiString("Ngày kiểm định không được để trống"),
                                        },
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
                            Description =
                                "Not Found - Inspection schedule or technician doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy lịch kiểm định"
                                        ),
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
