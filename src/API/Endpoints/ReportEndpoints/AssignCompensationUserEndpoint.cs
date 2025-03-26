using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ReportEndpoints;

public class AssignCompensationUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/reports/{reportId}/compensation", Handle)
            .WithSummary("Assign compensation user for a report")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Assign a user to pay compensation for a report.

                    Process:
                    1. Validates consultant permissions
                    2. Assigns compensation user and amount
                    3. Sets 5-day payment deadline
                    4. Sends email notification to assigned user
                    5. Schedules automatic ban if not paid

                    Note: Only consultants can assign compensation users
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Compensation user assigned",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["reportId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Người dùng đã được gán để thanh toán báo cáo"
                                        )
                                    }
                                }
                            }
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
                                            "Số tiền bồi thường không hợp lệ"
                                        )
                                    }
                                }
                            }
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
                                            "Bạn không có quyền thực hiện hành động này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Report or user not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy báo cáo hoặc người dùng"
                                        )
                                    }
                                }
                            }
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Report already processed",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Báo cáo chưa được xem xét")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid reportId,
        AssignCompensationUserRequest request
    )
    {
        Result<AssignCompensationUser.Response> result = await sender.Send(
            new AssignCompensationUser.Command(
                reportId,
                request.UserId,
                request.CompensationReason,
                request.CompensationAmount
            )
        );

        return result.MapResult();
    }

    private sealed record AssignCompensationUserRequest(
        Guid UserId,
        string CompensationReason,
        decimal CompensationAmount
    );
}
