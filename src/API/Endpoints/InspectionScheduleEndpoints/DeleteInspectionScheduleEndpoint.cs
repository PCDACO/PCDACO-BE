using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class DeleteInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/inspection-schedules/{id:guid}", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Delete an inspection schedule")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Remove an inspection schedule from the system (soft delete).

                    Deletion Rules:
                    - Only consultants can delete inspection schedules
                    - The schedule must be in 'Pending' status
                    - The schedule cannot be deleted if the inspection date is less than 1 day away

                    Process:
                    1. Validates consultant permissions
                    2. Verifies the schedule exists and is in pending status
                    3. Checks that the scheduled date is more than 1 day in the future
                    4. Soft deletes the schedule by marking it as deleted in the database
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Inspection schedule deleted",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Xóa thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad Request - Schedule can't be deleted due to status or timing",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể xóa lịch kiểm định có ngày kiểm định cách ngày hiện tại ít hơn 1 ngày"
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
                            Description = "Not Found - Inspection schedule doesn't exist",
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
                        ["409"] = new()
                        {
                            Description = "Conflict - Schedule is not in pending status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Chỉ có thể xóa lịch kiểm định đang chờ duyệt"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteInspectionSchedule.Command(id));
        return result.MapResult();
    }
}
