using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class CreateCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars", Handle)
            .WithSummary("Create a new car listing")
            .WithTags("Cars")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new car listing in the system.

                    Access Control:
                    - Requires authentication
                    - Only available to users with Owner role
                    - Not available to Admin users

                    Features:
                    - Automatic contract creation with owner signature
                    - License plate encryption
                    - Location tracking setup
                    - Statistics initialization
                    - Idempotency support to prevent duplicate submissions

                    Required Information:
                    - Car specifications (model, transmission, fuel type)
                    - Basic details (license plate, color, seats)
                    - Pricing and terms
                    - Pickup location
                    - Optional amenities

                    Validation Rules:
                    - License plate: 8-11 characters
                    - Seats: 1-49
                    - Description: max 500 characters
                    - Fuel consumption: must be positive
                    - Price: must be positive
                    - All required fields must be provided
                    """,

                    Responses =
                    {
                        ["201"] = new()
                        {
                            Description = "Created",
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
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Tạo thành công")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Validation errors"),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiObject
                                            {
                                                ["licensePlate"] = new OpenApiArray
                                                {
                                                    new OpenApiString(
                                                        "Biển số xe không được để trống !"
                                                    )
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to create cars",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện thao tác này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Referenced entities not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy loại nhiên liệu"
                                        )
                                    }
                                }
                            }
                        }
                    },

                    RequestBody = new()
                    {
                        Description = "Car creation details",
                        Required = true,
                        Content =
                        {
                            ["application/json"] = new()
                            {
                                Example = new OpenApiObject
                                {
                                    ["amenityIds"] = new OpenApiArray
                                    {
                                        new OpenApiString("123e4567-e89b-12d3-a456-426614174001"),
                                        new OpenApiString("123e4567-e89b-12d3-a456-426614174002")
                                    },
                                    ["modelId"] = new OpenApiString(
                                        "123e4567-e89b-12d3-a456-426614174003"
                                    ),
                                    ["transmissionTypeId"] = new OpenApiString(
                                        "123e4567-e89b-12d3-a456-426614174004"
                                    ),
                                    ["fuelTypeId"] = new OpenApiString(
                                        "123e4567-e89b-12d3-a456-426614174005"
                                    ),
                                    ["licensePlate"] = new OpenApiString("51G-123.45"),
                                    ["color"] = new OpenApiString("Black"),
                                    ["seat"] = new OpenApiInteger(4),
                                    ["description"] = new OpenApiString(
                                        "Well-maintained family sedan"
                                    ),
                                    ["fuelConsumption"] = new OpenApiDouble(7.5),
                                    ["requiresCollateral"] = new OpenApiBoolean(true),
                                    ["price"] = new OpenApiDouble(800000),
                                    ["terms"] = new OpenApiString("Standard rental terms apply"),
                                    ["pickupLatitude"] = new OpenApiDouble(10.762622),
                                    ["pickupLongitude"] = new OpenApiDouble(106.660172),
                                    ["pickupAddress"] = new OpenApiString("123 Main Street, City")
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, CreateCarRequest request)
    {
        Result<CreateCar.Response> result = await sender.Send(
            new CreateCar.Command(
                AmenityIds: request.AmenityIds,
                ModelId: request.ModelId,
                TransmissionTypeId: request.TransmissionTypeId,
                FuelTypeId: request.FuelTypeId,
                LicensePlate: request.LicensePlate,
                Color: request.Color,
                Seat: request.Seat,
                Description: request.Description,
                FuelConsumption: request.FuelConsumption,
                RequiresCollateral: request.RequiresCollateral,
                Price: request.Price,
                Terms: request.Terms,
                PickupLatitude: request.PickupLatitude,
                PickupLongitude: request.PickupLongitude,
                PickupAddress: request.PickupAddress
            )
        );
        return result.MapResult();
    }

    private sealed record CreateCarRequest(
        Guid[] AmenityIds,
        Guid ModelId,
        Guid TransmissionTypeId,
        Guid FuelTypeId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
        decimal PickupLatitude,
        decimal PickupLongitude,
        string PickupAddress,
        string Terms = ""
    );
}
