using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class InProgressInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inspection-schedules/{id:guid}/inprogress", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Mark an inspection schedule as in progress")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Change the status of an inspection schedule to "In Progress".

                    Features:
                    - Technicians can mark a scheduled inspection as started/in progress
                    - Can only update schedules that are in "Pending" status
                    - Schedule must not be expired (not more than 15 minutes past scheduled time)

                    Notes:
                    - Only users with technician role can update inspection schedules
                    - After update, the schedule's status will be InProgress
                    - The schedule's updatedAt timestamp will be updated to current time
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Inspection schedule updated to in progress",
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
                            Description =
                                "Bad Request - Schedule cannot be updated due to status or expired",
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
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a technician",
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
                            Description = "Conflict - Inspection schedule is expired",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Chỉ được thực hiện kiểm định trong khoảng 15 phút sau thời gian kiểm định"
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
        Result<InProgressInspectionSchedule.Response> result = await sender.Send(
            new InProgressInspectionSchedule.Command(id)
        );
        return result.MapResult();
    }
}
