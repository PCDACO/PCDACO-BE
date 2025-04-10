using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Constants;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class GetGPSDeviceDetailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gps-devices/{id:guid}", Handle)
            .WithSummary("Get GPS Device Detail")
            .WithTags("GPS Devices")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific GPS device including its associated car.

                    Access Control:
                    - Requires authentication
                    - Only Admin and Technician roles have access to this endpoint

                    Details Included:
                    - GPS device basic information (ID, OSBuildId, Name, Status)
                    - Associated car details (if assigned to a car)
                    - Car owner information with decrypted phone number
                    - Car statistics including total bookings, earnings, and average rating
                    - Current location information (if available)
                    - Pickup location data
                    - Associated manufacturer details
                    - Car images and amenities
                    - All car bookings
                    - Contract details (if available)

                    Notes:
                    - If the device is not assigned to any car, the CarDetail field will be null
                    - Sensitive information like owner's phone number is automatically decrypted
                    - Created timestamp is derived from the device's UUID
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
                                            ["osBuildId"] = new OpenApiString("ABCDEF123456"),
                                            ["name"] = new OpenApiString(
                                                "GPS Device - Toyota Camry"
                                            ),
                                            ["status"] = new OpenApiString("InUsed"),
                                            ["createdAt"] = new OpenApiString(
                                                "2024-01-01T00:00:00Z"
                                            ),
                                            ["carDetail"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174001"
                                                ),
                                                ["modelId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174002"
                                                ),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["releaseDate"] = new OpenApiString(
                                                    "2023-01-01T00:00:00Z"
                                                ),
                                                ["color"] = new OpenApiString("Black"),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["seat"] = new OpenApiInteger(4),
                                                ["description"] = new OpenApiString(
                                                    "Well-maintained family sedan"
                                                ),
                                                ["transmissionId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
                                                ),
                                                ["fuelTypeId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174004"
                                                ),
                                                ["fuelType"] = new OpenApiString("Gasoline"),
                                                ["fuelConsumption"] = new OpenApiDouble(7.5),
                                                ["requiresCollateral"] = new OpenApiBoolean(true),
                                                ["price"] = new OpenApiDouble(800000),
                                                ["terms"] = new OpenApiString(
                                                    "Standard rental terms"
                                                ),
                                                ["status"] = new OpenApiString("Available"),
                                                ["owner"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174005"
                                                    ),
                                                    ["name"] = new OpenApiString("John Smith"),
                                                    ["email"] = new OpenApiString(
                                                        "john.smith@example.com"
                                                    ),
                                                    ["phone"] = new OpenApiString("0987654321"),
                                                    ["address"] = new OpenApiString(
                                                        "123 Main Street, City"
                                                    ),
                                                    ["avatarUrl"] = new OpenApiString(
                                                        "https://example.com/avatar.jpg"
                                                    ),
                                                },
                                                ["statistics"] = new OpenApiObject
                                                {
                                                    ["totalBookings"] = new OpenApiInteger(15),
                                                    ["totalEarnings"] = new OpenApiDouble(12500000),
                                                    ["averageRating"] = new OpenApiDouble(4.5),
                                                    ["lastRented"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                },
                                                ["location"] = new OpenApiObject
                                                {
                                                    ["longtitude"] = new OpenApiDouble(106.660172),
                                                    ["latitude"] = new OpenApiDouble(10.762622),
                                                },
                                                ["pickupLocation"] = new OpenApiObject
                                                {
                                                    ["longitude"] = new OpenApiDouble(106.660172),
                                                    ["latitude"] = new OpenApiDouble(10.762622),
                                                    ["address"] = new OpenApiString(
                                                        "123 Main Street, City"
                                                    ),
                                                },
                                                ["manufacturer"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174006"
                                                    ),
                                                    ["name"] = new OpenApiString("Toyota"),
                                                },
                                                ["images"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174007"
                                                        ),
                                                        ["url"] = new OpenApiString(
                                                            "https://example.com/car1.jpg"
                                                        ),
                                                        ["type"] = new OpenApiString("exterior"),
                                                        ["name"] = new OpenApiString("Front view"),
                                                    },
                                                },
                                                ["amenities"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174008"
                                                        ),
                                                        ["name"] = new OpenApiString(
                                                            "GPS Navigation"
                                                        ),
                                                        ["description"] = new OpenApiString(
                                                            "Built-in GPS navigation system"
                                                        ),
                                                        ["icon"] = new OpenApiString(
                                                            "gps-icon.png"
                                                        ),
                                                    },
                                                },
                                                ["bookings"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["bookingId"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174009"
                                                        ),
                                                        ["driverId"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174010"
                                                        ),
                                                        ["driverName"] = new OpenApiString(
                                                            "Jane Doe"
                                                        ),
                                                        ["avatarUrl"] = new OpenApiString(
                                                            "https://example.com/jane.jpg"
                                                        ),
                                                        ["startTime"] = new OpenApiString(
                                                            "2024-03-20T10:00:00Z"
                                                        ),
                                                        ["endTime"] = new OpenApiString(
                                                            "2024-03-21T10:00:00Z"
                                                        ),
                                                    },
                                                },
                                                ["contract"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174011"
                                                    ),
                                                    ["terms"] = new OpenApiString(
                                                        "Contract terms and conditions"
                                                    ),
                                                    ["status"] = new OpenApiString("Active"),
                                                    ["ownerSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["technicianSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T11:00:00Z"
                                                    ),
                                                    ["inspectionResults"] = new OpenApiString(
                                                        "Car passed inspection"
                                                    ),
                                                    ["gpsDeviceId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                },
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User does not have Admin or Technician role",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            ResponseMessages.ForbiddenAudit
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - GPS device does not exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            ResponseMessages.GPSDeviceNotFound
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetGPSDeviceDetail.Response> result = await sender.Send(
            new GetGPSDeviceDetail.Query(id)
        );
        return result.MapResult();
    }
}
