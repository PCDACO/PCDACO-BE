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
                    - Location-based (latitude, longitude, radius)
                    - Car model
                    - Amenities
                    - Fuel type
                    - Transmission type
                    - Date range availability
                    - Keyword search

                    Pagination:
                    - Uses cursor-based pagination with lastId
                    - Configurable limit (default: 10)

                    Details Included:
                    - Car information (model, specifications, price)
                    - Owner details
                    - Location details
                    - Images
                    - Amenities
                    - Statistics (total rentals, average rating)

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
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
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
                                                    ["latitude"] = new OpenApiDouble(10.762622),
                                                    ["longitude"] = new OpenApiDouble(106.660172)
                                                }
                                            }
                                        },
                                        ["total"] = new OpenApiInteger(100),
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
                                    }
                                }
                            }
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
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "latitude")] decimal? latitude,
        [FromQuery(Name = "longtitude")] decimal? longtitude,
        [FromQuery(Name = "radius")] decimal? radius,
        [FromQuery(Name = "model")] Guid? model,
        [FromQuery(Name = "lastId")] Guid? lastCarId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "startTime")] DateTimeOffset? startTime,
        [FromQuery(Name = "endTime")] DateTimeOffset? endTime,
        [FromQuery(Name = "limit")] int? limit = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetCars.Response>> result = await sender.Send(
            new GetCars.Query(
                latitude,
                longtitude,
                radius,
                model,
                amenities,
                fuel,
                transmission,
                lastCarId,
                limit!.Value,
                keyword ?? "",
                startTime,
                endTime
            )
        );
        return result.MapResult();
    }
}
