using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Queries;

namespace API.Endpoints.BookingEndpoints;

public class GetBookingByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id}", Handle)
            .WithSummary("Get booking details by ID")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific booking.

                    Access Control:
                    - Admin: Can view any booking
                    - Driver: Can only view their own bookings
                    - Owner: Can only view bookings for their cars

                    Details Included:
                    - Car information (model, license plate, specifications)
                    - Driver details
                    - Owner details
                    - Booking information (dates, status, notes)
                    - Payment details (prices, fees, payment status)
                    - Trip information (distance)
                    - Feedback from both parties

                    Note: License plate is encrypted and will be decrypted for authorized users
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
                                            ["car"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174001"
                                                ),
                                                ["modelName"] = new OpenApiString("Toyota Camry"),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["color"] = new OpenApiString("Black"),
                                                ["seat"] = new OpenApiInteger(4),
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
                                                ),
                                                ["fuelType"] = new OpenApiString("Gasoline")
                                            },
                                            ["driver"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174002"
                                                ),
                                                ["name"] = new OpenApiString("John Doe"),
                                                ["phone"] = new OpenApiString("0123456789"),
                                                ["email"] = new OpenApiString("john@example.com")
                                            },
                                            ["owner"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["name"] = new OpenApiString("Jane Smith"),
                                                ["phone"] = new OpenApiString("0987654321"),
                                                ["email"] = new OpenApiString("jane@example.com")
                                            },
                                            ["booking"] = new OpenApiObject
                                            {
                                                ["startTime"] = new OpenApiString(
                                                    "2024-03-15T10:00:00Z"
                                                ),
                                                ["endTime"] = new OpenApiString(
                                                    "2024-03-20T10:00:00Z"
                                                ),
                                                ["actualReturnTime"] = new OpenApiString(
                                                    "2024-03-20T09:30:00Z"
                                                ),
                                                ["totalDistance"] = new OpenApiDouble(150.5),
                                                ["status"] = new OpenApiString("Completed"),
                                                ["note"] = new OpenApiString(
                                                    "Car returned in good condition"
                                                )
                                            },
                                            ["payment"] = new OpenApiObject
                                            {
                                                ["basePrice"] = new OpenApiDouble(2000000),
                                                ["platformFee"] = new OpenApiDouble(200000),
                                                ["excessDay"] = new OpenApiDouble(0),
                                                ["excessDayFee"] = new OpenApiDouble(0),
                                                ["totalAmount"] = new OpenApiDouble(2200000),
                                                ["isPaid"] = new OpenApiBoolean(true)
                                            },
                                            ["trip"] = new OpenApiObject
                                            {
                                                ["totalDistance"] = new OpenApiDouble(150.5)
                                            },
                                            ["feedbacks"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174004"
                                                    ),
                                                    ["rating"] = new OpenApiInteger(5),
                                                    ["content"] = new OpenApiString(
                                                        "Great experience!"
                                                    ),
                                                    ["type"] = new OpenApiString("DriverToOwner"),
                                                    ["userName"] = new OpenApiString("John Doe")
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view this booking",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền xem thông tin này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Booking doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy booking")
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
        var result = await sender.Send(new GetBookingById.Query(id), cancellationToken);
        return result.MapResult();
    }
}
