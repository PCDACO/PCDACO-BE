using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ReportEndpoints;

public class ReviewReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/reports/{reportId}/review", Handle)
            .WithSummary("Review a report")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Start reviewing a report.

                    Process:
                    1. Validates consultant permissions
                    2. Changes report status to UnderReview
                    3. Allows compensation assignment

                    Note: Only consultants can review reports
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Report is now under review",
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
                                            ),
                                            ["status"] = new OpenApiString("UnderReview")
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Báo cáo đang được xem xét")
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
                            Description = "Conflict - Report already processed",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Báo cáo đã được xử lý")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid reportId)
    {
        Result<ReviewReport.Response> result = await sender.Send(
            new ReviewReport.Command(reportId)
        );

        return result.MapResult();
    }
}
