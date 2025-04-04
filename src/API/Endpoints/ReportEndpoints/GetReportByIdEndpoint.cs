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
                    - Consultant: Can view any report
                    - Driver: Can only view reports related to their bookings
                    - Owner: Can only view reports related to their cars
                    - Reporter: Can view reports they created

                    Details Included:
                    - Basic report information (ID, title, description, type, status)
                    - Reporter information
                    - Resolution details (if resolved)
                    - Booking details (times, amounts, driver and owner info)
                    - Car details (model, manufacturer, color, images)
                    - Compensation details (if applicable)
                    - Inspection schedule details (if applicable)

                    Note:
                    - Sensitive information (license plates, phone numbers) is encrypted and decrypted for authorized users
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
                                            ["reporterId"] = new OpenApiString(Guid.NewGuid().ToString()),
                                            ["reporterName"] = new OpenApiString("Nguyễn Văn A"),
                                            ["title"] = new OpenApiString("Báo cáo hư hỏng xe"),
                                            ["description"] = new OpenApiString("Xe bị trầy xước nhẹ"),
                                            ["reportType"] = new OpenApiString("CarDamage"),
                                            ["status"] = new OpenApiString("Pending"),
                                            ["resolvedAt"] = new OpenApiNull(),
                                            ["resolvedById"] = new OpenApiNull(),
                                            ["resolutionComments"] = new OpenApiNull(),
                                            ["imageUrls"] = new OpenApiArray
                                            {
                                                new OpenApiString("https://example.com/report1.jpg")
                                            },
                                            ["bookingDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["driverId"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["driverName"] = new OpenApiString("Nguyễn Văn B"),
                                                ["driverAvatar"] = new OpenApiString("https://example.com/avatar1.jpg"),
                                                ["driverPhone"] = new OpenApiString("0123456789"),
                                                ["ownerId"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["ownerName"] = new OpenApiString("Trần Văn C"),
                                                ["ownerAvatar"] = new OpenApiString("https://example.com/avatar2.jpg"),
                                                ["ownerPhone"] = new OpenApiString("0987654321"),
                                                ["startTime"] = new OpenApiString(DateTime.UtcNow.ToString("o")),
                                                ["endTime"] = new OpenApiString(DateTime.UtcNow.AddDays(3).ToString("o")),
                                                ["totalAmount"] = new OpenApiDouble(2200000),
                                                ["basePrice"] = new OpenApiDouble(2000000)
                                            },
                                            ["carDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["manufacturerName"] = new OpenApiString("Toyota"),
                                                ["color"] = new OpenApiString("Đen"),
                                                ["imageUrl"] = new OpenApiArray
                                                {
                                                    new OpenApiString("https://example.com/car1.jpg")
                                                }
                                            },
                                            ["compensationDetail"] = new OpenApiObject
                                            {
                                                ["userId"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["userName"] = new OpenApiString("Nguyễn Văn D"),
                                                ["userAvatar"] = new OpenApiString("https://example.com/avatar3.jpg"),
                                                ["compensationReason"] = new OpenApiString("Trầy xước xe"),
                                                ["compensationAmount"] = new OpenApiDouble(500000),
                                                ["isPaid"] = new OpenApiBoolean(false),
                                                ["imageUrl"] = new OpenApiNull(),
                                                ["paidAt"] = new OpenApiNull()
                                            },
                                            ["inspectionScheduleDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["technicianId"] = new OpenApiString(Guid.NewGuid().ToString()),
                                                ["technicianName"] = new OpenApiString("Lê Văn E"),
                                                ["technicianAvatar"] = new OpenApiString("https://example.com/avatar4.jpg"),
                                                ["status"] = new OpenApiString("Scheduled"),
                                                ["inspectionAddress"] = new OpenApiString("123 Đường ABC, Quận 1, TP.HCM"),
                                                ["inspectionDate"] = new OpenApiString(DateTime.UtcNow.AddDays(1).ToString("o")),
                                                ["note"] = new OpenApiString("Kiểm tra trầy xước"),
                                                ["photoUrls"] = new OpenApiArray
                                                {
                                                    new OpenApiString("https://example.com/inspection1.jpg")
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
                                        ["message"] = new OpenApiString("Bạn không có quyền xem báo cáo này")
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
        var result = await sender.Send(new GetReportById.Query(id), cancellationToken);
        return result.MapResult();
    }
}
