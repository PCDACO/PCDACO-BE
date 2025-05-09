using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars", Handle)
            .WithSummary("Get available cars with filtering options")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of available cars with various filtering options.

                    Access Control:
                    - Requires authentication
                    - Available to all authenticated users

                    Filter Options:
                    - Location-based (latitude, longitude, radius in meters)
                    - Car model manufacturer
                    - Amenities (all specified amenities must be present)
                    - Fuel type
                    - Transmission type
                    - Date range availability (excludes cars with bookings in the specified period)
                    - Keyword search (matches model, description, color, manufacturer, license plate)

                    Pagination:
                    - Uses offset-based pagination
                    - Configurable page number and size (default: page 1, size 10)
                    - Results ordered by owner ratings and car ID

                    Details Included:
                    - Car information (model, specifications, price)
                    - Owner details
                    - Location details (GPS coordinates if available)
                    - Images with type information
                    - Amenities with full details
                    - Statistics (total rentals, average rating)

                    Note: 
                    - License plate is encrypted and will be decrypted for authorized users
                    - Cars with status other than 'Available' are excluded
                    - If date range is specified, cars with booking conflicts will show as 'Rented'
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
                                                ["ownerAvatarUrl"] = new OpenApiString(
                                                    "https://example.com/avatar.jpg"
                                                ),
                                                ["licensePlate"] = new OpenApiString("51G-123.45"),
                                                ["color"] = new OpenApiString("Black"),
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
                                                    "Standard rental terms apply"
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
                                            "Invalid parameters provided"
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
        [FromQuery(Name = "latitude")] decimal? latitude,
        [FromQuery(Name = "longtitude")] decimal? longtitude,
        [FromQuery(Name = "radius")] decimal? radius,
        [FromQuery(Name = "manufacturerId")] Guid? manufacturerId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "startTime")] DateTimeOffset? startTime,
        [FromQuery(Name = "endTime")] DateTimeOffset? endTime,
        [FromQuery(Name = "keyword")] string? keyword = "",
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10
    )
    {
        Result<OffsetPaginatedResponse<GetCars.Response>> result = await sender.Send(
            new GetCars.Query(
                latitude,
                longtitude,
                radius,
                manufacturerId,
                amenities,
                fuel,
                transmission,
                keyword ?? "",
                startTime,
                endTime,
                pageNumber ?? 1,
                pageSize ?? 10
            )
        );
        return result.MapResult();
    }
}
