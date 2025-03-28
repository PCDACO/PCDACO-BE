using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ReportEndpoints;

public class ApproveReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/reports/{reportId}/approve", Handle)
            .WithSummary("Approve or reject a report")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Approve or reject a report that has been reviewed.

                    Process:
                    1. Validates consultant/admin permissions
                    2. Updates report status to Resolved or Rejected
                    3. Records resolution details and timestamp
                    4. If approved and compensation was required:
                       - Marks compensation as paid
                       - Unbans user if they were banned for late payment
                    5. If rejected:
                       - Resets compensation payment status
                       - Keeps user banned if they were banned for late payment

                    Note: Only consultants and admins can approve/reject reports
                    """,

                    RequestBody = new()
                    {
                        Content =
                        {
                            ["application/json"] = new()
                            {
                                Example = new OpenApiObject
                                {
                                    ["isApproved"] = new OpenApiBoolean(true),
                                    ["note"] = new OpenApiString("Resolution notes here")
                                }
                            }
                        }
                    },

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Report has been processed",
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
                                            ["title"] = new OpenApiString("Report Title"),
                                            ["description"] = new OpenApiString(
                                                "Report Description"
                                            ),
                                            ["status"] = new OpenApiString("Resolved"),
                                            ["resolutionComments"] = new OpenApiString(
                                                "Resolution notes"
                                            ),
                                            ["resolvedAt"] = new OpenApiString(
                                                "2024-03-20T10:00:00Z"
                                            ),
                                            ["isCompensationPaid"] = new OpenApiBoolean(true),
                                            ["compensationAmount"] = new OpenApiDouble(100.00),
                                            ["compensationReason"] = new OpenApiString(
                                                "Damage compensation"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Báo cáo đã được xử lý thành công"
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
                                            "Ghi chú xử lý không được để trống"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a consultant or admin",
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
                            Description = "Not Found - Report not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy báo cáo")
                                    }
                                }
                            }
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Report not under review",
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
        ApproveReportRequest request
    )
    {
        Result<ApproveReport.Response> result = await sender.Send(
            new ApproveReport.Command(reportId, request.IsApproved, request.Note)
        );

        return result.MapResult();
    }

    private sealed record ApproveReportRequest(bool IsApproved, string Note);
}
