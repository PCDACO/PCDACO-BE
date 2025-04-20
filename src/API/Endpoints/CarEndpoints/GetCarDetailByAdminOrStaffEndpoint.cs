using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarDetailByAdminOrStaffEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car/{id}/admin", Handle)
            .WithSummary(
                "Get detailed car information by ID for admin web (Admin or staff can use)"
            )
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve comprehensive details about a specific car for admin or staff.

                    Access Control:
                    - Requires authentication
                    - Restricted to Admin or Consultant or Technician users
                    - Not-allowed users will receive a 403 Forbidden response

                    Details Included:
                    - Full car specifications (model, color, seats, transmission, fuel type)
                    - Owner information including contact details
                    - Statistics (total bookings, earnings, ratings, last rental date)
                    - Location details (current GPS and pickup location)
                    - Images and amenities with full details
                    - Recent bookings (top 4 most recent)
                    - Driver feedbacks for this car
                    - Contract details if available (terms, signatures, inspection results)

                    Note: All sensitive information including license plates are automatically decrypted
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
                                            ["modelId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["modelName"] = new OpenApiString("Toyota Camry"),
                                            ["releaseDate"] = new OpenApiString(
                                                "2020-01-01T00:00:00Z"
                                            ),
                                            ["color"] = new OpenApiString("Black"),
                                            ["licensePlate"] = new OpenApiString("51G-123.45"),
                                            ["seat"] = new OpenApiInteger(4),
                                            ["description"] = new OpenApiString(
                                                "Well-maintained family sedan"
                                            ),
                                            ["transmissionId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174008"
                                            ),
                                            ["transmissionType"] = new OpenApiString("Automatic"),
                                            ["fuelTypeId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174009"
                                            ),
                                            ["fuelType"] = new OpenApiString("Gasoline"),
                                            ["fuelConsumption"] = new OpenApiDouble(7.5),
                                            ["requiresCollateral"] = new OpenApiBoolean(true),
                                            ["price"] = new OpenApiDouble(800000),
                                            ["terms"] = new OpenApiString(
                                                "Standard rental terms apply"
                                            ),
                                            ["status"] = new OpenApiString("Available"),
                                            ["owner"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174002"
                                                ),
                                                ["name"] = new OpenApiString("John Smith"),
                                                ["email"] = new OpenApiString("john@example.com"),
                                                ["phone"] = new OpenApiString("0987654321"),
                                                ["address"] = new OpenApiString("123 Main St"),
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
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["name"] = new OpenApiString("Toyota"),
                                            },
                                            ["images"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174004"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://example.com/car.jpg"
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
                                                        "123e4567-e89b-12d3-a456-426614174005"
                                                    ),
                                                    ["name"] = new OpenApiString("GPS"),
                                                    ["description"] = new OpenApiString(
                                                        "Built-in GPS navigation"
                                                    ),
                                                    ["icon"] = new OpenApiString("gps-icon.png"),
                                                },
                                            },
                                            ["bookings"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["bookingId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174010"
                                                    ),
                                                    ["driverId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174011"
                                                    ),
                                                    ["driverName"] = new OpenApiString("Jane Doe"),
                                                    ["avatarUrl"] = new OpenApiString(
                                                        "https://example.com/jane.jpg"
                                                    ),
                                                    ["startTime"] = new OpenApiString(
                                                        "2024-03-20T10:00:00Z"
                                                    ),
                                                    ["endTime"] = new OpenApiString(
                                                        "2024-03-25T10:00:00Z"
                                                    ),
                                                    ["amount"] = new OpenApiDouble(1500000),
                                                    ["status"] = new OpenApiString("Completed"),
                                                },
                                            },
                                            ["feedbacks"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174010"
                                                    ),
                                                    ["userId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174011"
                                                    ),
                                                    ["userName"] = new OpenApiString(
                                                        "Alex Johnson"
                                                    ),
                                                    ["userAvatar"] = new OpenApiString(
                                                        "https://example.com/avatar2.jpg"
                                                    ),
                                                    ["rating"] = new OpenApiInteger(5),
                                                    ["content"] = new OpenApiString(
                                                        "Excellent car, very clean and well maintained!"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2024-02-25T14:30:00Z"
                                                    ),
                                                },
                                            },
                                            ["contract"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174006"
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
                                                    "123e4567-e89b-12d3-a456-426614174007"
                                                ),
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
                            Description = "Forbidden - User is not an admin",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện kiểm soát"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe"),
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
        Result<GetCarDetailByAdminOrStaff.Response> result = await sender.Send(
            new GetCarDetailByAdminOrStaff.Query(id)
        );
        return result.MapResult();
    }
}
