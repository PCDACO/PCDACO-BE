using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_CarReport.Queries;

namespace API.Endpoints.CarReportEndpoints;

public class GetCarReportByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car-reports/{id}", Handle)
            .WithSummary("Get car report details by ID")
            .WithTags("Car Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific car report.

                    Access Control:
                    - Admin: Can view any report
                    - Consultant: Can view any report
                    - Owner: Can only view reports related to their cars
                    - Reporter: Can view reports they created

                    Details Included:
                    - Basic report information (ID, title, description, type, status)
                    - Reporter information
                    - Resolution details (if resolved)
                    - Car details (model, manufacturer, color, images)
                    - Inspection schedule details (if applicable)

                    Note:
                    - Sensitive information (license plates) is encrypted and decrypted for authorized users
                    - Image URLs are included for report images, car images, and inspection photos
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
                                            ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                                            ["reporterId"] = new OpenApiString(
                                                Guid.NewGuid().ToString()
                                            ),
                                            ["reporterName"] = new OpenApiString("Nguyễn Văn A"),
                                            ["reporterRole"] = new OpenApiString("Owner"),
                                            ["title"] = new OpenApiString("Báo cáo hư hỏng xe"),
                                            ["description"] = new OpenApiString(
                                                "Xe bị trầy xước nhẹ"
                                            ),
                                            ["reportType"] = new OpenApiString("BodyDamage"),
                                            ["status"] = new OpenApiString("Pending"),
                                            ["resolvedAt"] = new OpenApiNull(),
                                            ["resolvedById"] = new OpenApiNull(),
                                            ["resolutionComments"] = new OpenApiNull(),
                                            ["imageUrls"] = new OpenApiArray
                                            {
                                                new OpenApiString("https://example.com/report1.jpg")
                                            },
                                            ["carDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    Guid.NewGuid().ToString()
                                                ),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["manufacturerName"] = new OpenApiString("Toyota"),
                                                ["color"] = new OpenApiString("Đen"),
                                                ["imageUrl"] = new OpenApiArray
                                                {
                                                    new OpenApiString(
                                                        "https://example.com/car1.jpg"
                                                    )
                                                }
                                            },
                                            ["inspectionScheduleDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    Guid.NewGuid().ToString()
                                                ),
                                                ["technicianId"] = new OpenApiString(
                                                    Guid.NewGuid().ToString()
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
                                                    DateTime.UtcNow.AddDays(1).ToString("o")
                                                ),
                                                ["note"] = new OpenApiString("Kiểm tra trầy xước"),
                                                ["photoUrls"] = new OpenApiArray
                                                {
                                                    new OpenApiString(
                                                        "https://example.com/inspection1.jpg"
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
                                            "Bạn không có quyền xem báo cáo này"
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
                                        ["message"] = new OpenApiString("Không tìm thấy báo cáo")
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
        var result = await sender.Send(new GetCarReportById.Query(id), cancellationToken);
        return result.MapResult();
    }
}
