using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Report.Queries;

namespace API.Endpoints.ReportEndpoints;

public class GetReportByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/{id}", Handle)
            .WithSummary("Get report details by ID")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific report.

                    Access Control:
                    - Admin: Can view any report
                    - Driver: Can only view reports related to their bookings
                    - Owner: Can only view reports related to their cars
                    - Reporter: Can view reports they created

                    Details Included:
                    - Report information (title, description, status)
                    - Reporter details
                    - Resolution details (if resolved)
                    - Related booking information
                    - Car information
                    - Driver and owner details
                    - Report images

                    Note: Sensitive information like license plates and phone numbers are encrypted and will be decrypted for authorized users
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
                                            ["id"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["reporterId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["reportedName"] = new OpenApiString("John Doe"),
                                            ["title"] = new OpenApiString("Issue with Car Return"),
                                            ["description"] = new OpenApiString(
                                                "Car was returned with minor scratches"
                                            ),
                                            ["reportType"] = new OpenApiString("CarDamage"),
                                            ["status"] = new OpenApiString("Pending"),
                                            ["resolvedAt"] = new OpenApiNull(),
                                            ["resolvedById"] = new OpenApiNull(),
                                            ["resolutionComments"] = new OpenApiNull(),
                                            ["imageReports"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174002"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://example.com/image1.jpg"
                                                    )
                                                }
                                            },
                                            ["bookingDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["driverId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174004"
                                                ),
                                                ["driverName"] = new OpenApiString("Jane Smith"),
                                                ["driverAvatar"] = new OpenApiString(
                                                    "https://example.com/avatar1.jpg"
                                                ),
                                                ["driverPhone"] = new OpenApiString("0123456789"),
                                                ["ownerId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174005"
                                                ),
                                                ["ownerName"] = new OpenApiString("Bob Wilson"),
                                                ["ownerAvatar"] = new OpenApiString(
                                                    "https://example.com/avatar2.jpg"
                                                ),
                                                ["ownerPhone"] = new OpenApiString("0987654321"),
                                                ["startTime"] = new OpenApiString(
                                                    "2024-03-15T10:00:00Z"
                                                ),
                                                ["endTime"] = new OpenApiString(
                                                    "2024-03-20T10:00:00Z"
                                                ),
                                                ["totalAmount"] = new OpenApiDouble(2200000),
                                                ["basePrice"] = new OpenApiDouble(2000000)
                                            },
                                            ["carDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174006"
                                                ),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["manufacturerName"] = new OpenApiString("Toyota"),
                                                ["color"] = new OpenApiString("Black"),
                                                ["imageUrl"] = new OpenApiArray
                                                {
                                                    new OpenApiString(
                                                        "https://example.com/car1.jpg"
                                                    )
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Fetched successfully")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view this report",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "You don't have permission to view this report"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Report doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Report not found")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        ISender sender,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new GetReportById.Query(id), cancellationToken);
        return result.MapResult();
    }
}
