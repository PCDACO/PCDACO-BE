using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Constants;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class ApproveInspectionScheduleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inspection-schedules/{id:guid}/approve", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Approve an inspection schedule")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allows a technician to approve or reject an inspection schedule after performing the inspection.

                    Requirements:
                    - Must be executed by a technician
                    - Only the assigned technician can approve/reject their own schedules
                    - The inspection schedule must be in InProgress status
                    - The current time cannot be more than 1 hour after the scheduled inspection time
                    - The car contract must be properly signed by both owner and technician

                    Process:
                    1. Updates the contract with inspection results and generates HTML contract
                    2. Updates the contract status to Completed
                    3. Updates the schedule status to Approved (if approved) or Rejected (if not)
                    4. Updates the car status to Available (if approved) or Rejected (if not)
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Inspection schedule approved or rejected",
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
                            Description = "Bad Request - Validation error",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString(
                                                "Id lịch kiểm định không được để trống"
                                            ),
                                            new OpenApiString(
                                                "Trạng thái phê duyệt không được để trống"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad request - Schedule in wrong state, expired, or contract issues",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            ResponseMessages.OnlyUpdateSignedOrInprogressInspectionSchedule
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not a technician or not assigned to this schedule",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không phải là kiểm định viên được chỉ định"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Schedule or contract not found",
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
        ApproveInspectionScheduleRequest request
    )
    {
        Result<ApproveInspectionSchedule.Response> result = await sender.Send(
            new ApproveInspectionSchedule.Command(
                Id: id,
                Note: request.Note,
                IsApproved: request.IsApproved
            )
        );
        return result.MapResult();
    }

    private record ApproveInspectionScheduleRequest(string Note, bool IsApproved);
}
