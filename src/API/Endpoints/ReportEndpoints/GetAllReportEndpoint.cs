using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Queries;

namespace API.Endpoints.ReportEndpoints;

public class GetAllReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports", Handle)
            .WithSummary("Get all reports")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve paginated list of reports with offset-based pagination.

                    Access Control:
                    - Drivers: Can only view reports they created or reports about their bookings
                    - Owners: Can only view reports they created or reports about their cars
                    - Admins/Consultants: Can view all reports

                    Filtering Options:
                    - Search: Filter by title, description, or reporter name
                    - Status: Filter by report status (Pending, InProgress, Resolved)
                    - Type: Filter by report type (Accident, Damage, Dispute, etc.)

                    Pagination:
                    - Offset-based pagination using page number and size
                    - Default: 10 items per page
                    - Returns total count and hasNext flag
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
                                        ["value"] = new OpenApiObject
                                        {
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["bookingId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["reporterId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174002"
                                                    ),
                                                    ["reportedName"] = new OpenApiString(
                                                        "John Doe"
                                                    ),
                                                    ["title"] = new OpenApiString(
                                                        "Car Accident Report"
                                                    ),
                                                    ["description"] = new OpenApiString(
                                                        "Minor collision at intersection"
                                                    ),
                                                    ["reportType"] = new OpenApiString("Accident"),
                                                    ["status"] = new OpenApiString("Pending"),
                                                    ["resolvedAt"] = new OpenApiNull(),
                                                    ["resolvedById"] = new OpenApiNull(),
                                                    ["resolutionComments"] = new OpenApiNull(),
                                                    ["imageReports"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "https://example.com/image1.jpg"
                                                        ),
                                                        new OpenApiString(
                                                            "https://example.com/image2.jpg"
                                                        )
                                                    }
                                                }
                                            },
                                            ["totalCount"] = new OpenApiInteger(50),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["hasNext"] = new OpenApiBoolean(true)
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy danh sách báo cáo thành công"
                                        )
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid parameters",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Invalid report status provided"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view these reports"
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? searchTerm = "",
        [FromQuery(Name = "status")] BookingReportStatus? status = null,
        [FromQuery(Name = "type")] BookingReportType? type = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await sender.Send(
            new GetAllReports.Query(pageNumber, pageSize, searchTerm, status, type),
            cancellationToken
        );

        return result.MapResult();
    }
}
