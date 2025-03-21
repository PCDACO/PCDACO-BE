using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car/{id}", Handle)
            .WithSummary("Get detailed car information by ID")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific car.

                    Access Control:
                    - Requires authentication
                    - Contract details are only visible to:
                      * Admins
                      * Consultants
                      * Technicians
                      * Car owner

                    Details Included:
                    - Car specifications (model, color, seats, etc.)
                    - Owner information
                    - Location details (current and pickup locations)
                    - Images and amenities
                    - Statistics (total rentals, average rating)
                    - Future booking schedule
                    - Contract details (for authorized users)

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
                                            ["modelId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["modelName"] = new OpenApiString("Toyota Camry"),
                                            ["ownerId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174002"
                                            ),
                                            ["ownerName"] = new OpenApiString("John Smith"),
                                            ["licensePlate"] = new OpenApiString("51G-123.45"),
                                            ["color"] = new OpenApiString("Black"),
                                            ["seat"] = new OpenApiInteger(4),
                                            ["description"] = new OpenApiString(
                                                "Well-maintained family sedan"
                                            ),
                                            ["transmissionType"] = new OpenApiString("Automatic"),
                                            ["fuelType"] = new OpenApiString("Gasoline"),
                                            ["fuelConsumption"] = new OpenApiDouble(7.5),
                                            ["requiresCollateral"] = new OpenApiBoolean(true),
                                            ["price"] = new OpenApiDouble(800000),
                                            ["terms"] = new OpenApiString(
                                                "Standard rental terms apply"
                                            ),
                                            ["status"] = new OpenApiString("Available"),
                                            ["totalRented"] = new OpenApiInteger(15),
                                            ["averageRating"] = new OpenApiDouble(4.5),
                                            ["location"] = new OpenApiObject
                                            {
                                                ["latitude"] = new OpenApiDouble(10.762622),
                                                ["longitude"] = new OpenApiDouble(106.660172)
                                            },
                                            ["pickupLocation"] = new OpenApiObject
                                            {
                                                ["latitude"] = new OpenApiDouble(10.762622),
                                                ["longitude"] = new OpenApiDouble(106.660172),
                                                ["address"] = new OpenApiString(
                                                    "123 Main Street, City"
                                                )
                                            },
                                            ["manufacturer"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174003"
                                                ),
                                                ["name"] = new OpenApiString("Toyota")
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
                                                    ["name"] = new OpenApiString("Front view")
                                                }
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
                                                    ["icon"] = new OpenApiString("gps-icon.png")
                                                }
                                            },
                                            ["bookings"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["startTime"] = new OpenApiString(
                                                        "2024-03-20T10:00:00Z"
                                                    ),
                                                    ["endTime"] = new OpenApiString(
                                                        "2024-03-25T10:00:00Z"
                                                    )
                                                }
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
                                                )
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
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
                                        ["message"] = new OpenApiString("Không tìm thấy xe")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetCarById.Response> result = await sender.Send(new GetCarById.Query(id));
        return result.MapResult();
    }
}
