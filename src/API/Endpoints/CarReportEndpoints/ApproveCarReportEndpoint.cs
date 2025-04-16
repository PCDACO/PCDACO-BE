using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_CarReport.Commands;

namespace API.Endpoints.CarReportEndpoints;

public class ApproveCarReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/car-reports/{reportId}/approve", Handle)
            .WithSummary("Approve or reject a car report")
            .WithTags("Car Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Approve or reject a car report.

                    Access Control:
                    - Requires authentication
                    - Only Admin or Consultant can approve/reject reports

                    Process:
                    1. Validates user permissions
                    2. Updates report status (Resolved/Rejected)
                    3. Updates car status to Maintain if approved
                    4. Records resolution comments and timestamp

                    Note:
                    - If approved, the car will be marked as in maintenance
                    - If rejected, the car status remains unchanged
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Report approved/rejected",
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
                                            ["title"] = new OpenApiString("Hư hỏng động cơ"),
                                            ["description"] = new OpenApiString(
                                                "Động cơ phát ra tiếng ồn lạ khi khởi động"
                                            ),
                                            ["status"] = new OpenApiString("Resolved"),
                                            ["resolutionComments"] = new OpenApiString(
                                                "Đã xác nhận hư hỏng, cần bảo dưỡng"
                                            ),
                                            ["resolvedAt"] = new OpenApiString(
                                                DateTimeOffset.UtcNow.ToString("o")
                                            ),
                                            ["inspectionScheduleDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174001"
                                                ),
                                                ["technicianId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174002"
                                                ),
                                                ["technicianName"] = new OpenApiString("Lê Văn E"),
                                                ["technicianAvatar"] = new OpenApiString(
                                                    "https://example.com/avatar4.jpg"
                                                ),
                                                ["status"] = new OpenApiString("Scheduled"),
                                                ["inspectionAddress"] = new OpenApiString(
                                                    "123 Đường ABC, Quận 1, TP.HCM"
                                                ),
                                                ["inspectionDate"] = new OpenApiString(
                                                    DateTimeOffset.UtcNow.AddDays(1).ToString("o")
                                                ),
                                                ["note"] = new OpenApiString("Kiểm tra động cơ"),
                                                ["photoUrls"] = new OpenApiArray
                                                {
                                                    new OpenApiString(
                                                        "https://example.com/inspection1.jpg"
                                                    )
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Báo cáo xe đã được xử lý thành công"
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
                                            "Validation errors:\n"
                                                + "- ID báo cáo không được để trống\n"
                                                + "- Ghi chú không được quá 1000 ký tự"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not Admin or Consultant",
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
                        }
                    },
                    RequestBody = new()
                    {
                        Description = "Approval details",
                        Required = true,
                        Content =
                        {
                            ["application/json"] = new()
                            {
                                Example = new OpenApiObject
                                {
                                    ["isApproved"] = new OpenApiBoolean(true),
                                    ["note"] = new OpenApiString(
                                        "Đã xác nhận hư hỏng, cần bảo dưỡng"
                                    )
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid reportId,
        ApproveCarReportRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new ApproveCarReport.Command(reportId, request.IsApproved, request.Note),
            cancellationToken
        );

        return result.MapResult();
    }

    private sealed record ApproveCarReportRequest(bool IsApproved, string Note);
}
