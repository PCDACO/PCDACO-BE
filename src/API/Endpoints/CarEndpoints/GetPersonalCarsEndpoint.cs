using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetPersonalCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/personal", Handle)
            .WithSummary("Get user's personal car listings")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of cars owned by the authenticated user.

                    Access Control:
                    - Requires authentication
                    - Users can only view their own cars
                    - Contract details visibility based on role:
                      * Admins
                      * Consultants
                      * Technicians
                      * Car owners

                    Filtering Options:
                    - Manufacturer (manufacturerId)
                    - Amenities (amenities[])
                    - Fuel type (fuel)
                    - Transmission type (transmission)
                    - Car status (status)
                    - Keyword search (keyword) - searches in model name

                    Pagination:
                    - Uses offset-based pagination
                    - Configurable page number and size (default: page 1, size 10)
                    - Sorted by owner rating and car ID (descending)

                    Details Included:
                    - Car specifications (model, fuel, transmission, seats, etc.)
                    - Location information (GPS coordinates if available)
                    - Statistics (total rentals, average rating)
                    - Images and amenities with full details
                    - Contract details (for authorized roles)
                    - Manufacturer details
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
                                        ["value"] = new OpenApiArray
                                        {
                                            new OpenApiObject
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
                                                ["transmissionTypeId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174010"
                                                ),
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
                                                ),
                                                ["fuelTypeId"] = new OpenApiString(
                                                    "123e4567-e89b-12d3-a456-426614174011"
                                                ),
                                                ["fuelType"] = new OpenApiString("Gasoline"),
                                                ["fuelConsumption"] = new OpenApiDouble(7.5),
                                                ["requiresCollateral"] = new OpenApiBoolean(true),
                                                ["price"] = new OpenApiDouble(800000),
                                                ["terms"] = new OpenApiString(
                                                    "Standard rental terms"
                                                ),
                                                ["status"] = new OpenApiString("Available"),
                                                ["totalRented"] = new OpenApiInteger(15),
                                                ["averageRating"] = new OpenApiDouble(4.5),
                                                ["location"] = new OpenApiObject
                                                {
                                                    ["longitude"] = new OpenApiDouble(106.660172),
                                                    ["latitude"] = new OpenApiDouble(10.762622),
                                                },
                                                ["manufacturer"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174005"
                                                    ),
                                                    ["name"] = new OpenApiString("Toyota"),
                                                },
                                                ["images"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174006"
                                                        ),
                                                        ["url"] = new OpenApiString(
                                                            "https://example.com/images/car1.jpg"
                                                        ),
                                                        ["type"] = new OpenApiString("Exterior"),
                                                        ["name"] = new OpenApiString("Front view"),
                                                    },
                                                },
                                                ["amenities"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174007"
                                                        ),
                                                        ["name"] = new OpenApiString(
                                                            "Air Conditioning"
                                                        ),
                                                        ["description"] = new OpenApiString(
                                                            "Climate control"
                                                        ),
                                                        ["icon"] = new OpenApiString("ac-icon"),
                                                    },
                                                },
                                                ["contract"] = new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174003"
                                                    ),
                                                    ["terms"] = new OpenApiString("Contract terms"),
                                                    ["status"] = new OpenApiString("Active"),
                                                    ["ownerSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["technicianSignatureDate"] = new OpenApiString(
                                                        "2024-03-15T11:00:00Z"
                                                    ),
                                                    ["inspectionResults"] = new OpenApiString(
                                                        "Passed inspection"
                                                    ),
                                                    ["gpsDeviceId"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174004"
                                                    ),
                                                },
                                            },
                                        },
                                        ["total"] = new OpenApiInteger(100),
                                        ["pageNumber"] = new OpenApiInteger(1),
                                        ["pageSize"] = new OpenApiInteger(10),
                                        ["hasNext"] = new OpenApiBoolean(true),
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view these cars",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "manufacturerId")] Guid? manufacturerId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "status")] CarStatusEnum? status,
        [FromQuery(Name = "keyword")] string? keyword = "",
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10
    )
    {
        Result<OffsetPaginatedResponse<GetPersonalCars.Response>> result = await sender.Send(
            new GetPersonalCars.Query(
                ManufacturerId: manufacturerId,
                Amenities: amenities,
                FuelTypes: fuel,
                TransmissionTypes: transmission,
                Status: status,
                Keyword: keyword,
                PageNumber: pageNumber ?? 1,
                PageSize: pageSize ?? 10
            )
        );
        return result.MapResult();
    }
}
