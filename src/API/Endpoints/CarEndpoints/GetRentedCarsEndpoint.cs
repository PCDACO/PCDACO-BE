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

public class GetRentedCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/rented", Handle)
            .WithSummary("Get list of currently rented cars")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of all currently rented cars in the system.

                    Access Control:
                    - Requires authentication
                    - Restricted to Admin users only

                    Features:
                    - Lists all cars with status 'Rented'
                    - Includes decrypted license plates for admin view
                    - Sorted by owner ratings and car ID

                    Pagination:
                    - Offset-based pagination
                    - Default page size: 10
                    - Default page number: 1

                    Filtering:
                    - Optional keyword search

                    Details Included:
                    - Car specifications (model, color, seats)
                    - Owner information
                    - Location details
                    - Manufacturer information
                    - Images and amenities
                    - Pricing and requirements
                    - Decrypted license plates
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
                                                ["transmissionType"] = new OpenApiString(
                                                    "Automatic"
                                                ),
                                                ["fuelType"] = new OpenApiString("Gasoline"),
                                                ["fuelConsumption"] = new OpenApiDouble(7.5),
                                                ["requiresCollateral"] = new OpenApiBoolean(true),
                                                ["price"] = new OpenApiDouble(800000),
                                                ["location"] = new OpenApiObject
                                                {
                                                    ["latitude"] = new OpenApiDouble(10.762622),
                                                    ["longitude"] = new OpenApiDouble(106.660172)
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
                                                        )
                                                    }
                                                }
                                            }
                                        },
                                        ["pageNumber"] = new OpenApiInteger(1),
                                        ["pageSize"] = new OpenApiInteger(10),
                                        ["totalItems"] = new OpenApiInteger(100),
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công")
                                    }
                                }
                            }
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
                                            "Bạn không có quyền thực hiện thao tác này"
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
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetRentedCars.Response>> result = await sender.Send(
            new GetRentedCars.Query(pageNumber, pageSize, keyword!)
        );
        return result.MapResult();
    }
}
