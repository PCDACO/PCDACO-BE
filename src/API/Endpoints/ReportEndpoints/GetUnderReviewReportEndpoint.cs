using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Queries;

namespace API.Endpoints.ReportEndpoints;

public class GetUnderReviewReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/under-review", Handle)
            .WithSummary("Get all reports under review by current consultant")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve list of reports that are currently under review by the authenticated consultant.

                    Access Control:
                    - Only Consultants can access this endpoint
                    - Consultants can only view reports they are assigned to review

                    Returns:
                    - List of reports with UnderReview status
                    - Each report includes basic information and creation timestamp
                    - Reports are ordered by creation time (newest first)
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success",
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
                                                    "123e4567-e89b-12d3-a456-426614174000"
                                                ),
                                                ["bookingId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174001"
                                                ),
                                                ["title"] = new OpenApiString(
                                                    "Car Accident Report"
                                                ),
                                                ["reportType"] = new OpenApiString("Accident"),
                                                ["description"] = new OpenApiString(
                                                    "Minor collision at intersection"
                                                ),
                                                ["status"] = new OpenApiString("UnderReview"),
                                                ["createdAt"] = new OpenApiString(
                                                    "2024-03-20T10:30:00Z"
                                                ),
                                                ["reportedByName"] = new OpenApiString("John Doe")
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy danh sách báo cáo đang xem xét thành công"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new() { Description = "Forbidden - User is not a consultant" }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken cancellationToken = default
    )
    {
        var result = await sender.Send(new GetUnderReviewReports.Query(), cancellationToken);

        return result.MapResult();
    }
}
