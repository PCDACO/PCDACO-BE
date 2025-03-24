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
                    Retrieve paginated list of reports with cursor-based pagination.

                    Access Control:
                    - Drivers: Can only view reports they created or reports about their bookings
                    - Owners: Can only view reports they created or reports about their cars
                    - Admins/Consultants: Can view all reports

                    Filtering Options:
                    - Search: Filter by title, description, or reporter name
                    - Status: Filter by report status (Pending, InProgress, Resolved)
                    - Type: Filter by report type (Accident, Damage, Dispute, etc.)

                    Pagination:
                    - Cursor-based pagination using lastId
                    - Default limit: 10 items per page
                    - Returns hasMore flag for additional pages
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
                                                        new OpenApiObject
                                                        {
                                                            ["id"] = new OpenApiString(
                                                                "123e4567-e89b-12d3-a456-426614174003"
                                                            ),
                                                            ["url"] = new OpenApiString(
                                                                "https://example.com/image.jpg"
                                                            )
                                                        }
                                                    }
                                                }
                                            },
                                            ["totalCount"] = new OpenApiInteger(50),
                                            ["limit"] = new OpenApiInteger(10),
                                            ["lastId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["hasMore"] = new OpenApiBoolean(true)
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
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "lastId")] Guid? lastId,
        [FromQuery(Name = "status")] BookingReportStatus? status,
        [FromQuery(Name = "type")] BookingReportType? type,
        [FromQuery(Name = "search")] string? searchTerm,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new GetAllReports.Query(limit ?? 10, lastId, status, type, searchTerm),
            cancellationToken
        );

        return result.MapResult();
    }
}
