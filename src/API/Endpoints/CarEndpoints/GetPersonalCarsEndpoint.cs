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
                    - Car model
                    - Amenities
                    - Fuel type
                    - Transmission type
                    - Car status

                    Pagination:
                    - Cursor-based pagination using lastId
                    - Configurable limit (default: 10)
                    - Sorted by owner rating and creation date

                    Details Included:
                    - Car specifications
                    - Location information
                    - Statistics (rentals, ratings)
                    - Images and amenities
                    - Contract details (for authorized roles)
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
                                                ["terms"] = new OpenApiString(
                                                    "Standard rental terms"
                                                ),
                                                ["status"] = new OpenApiString("Available"),
                                                ["totalRented"] = new OpenApiInteger(15),
                                                ["averageRating"] = new OpenApiDouble(4.5),
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
                                                    )
                                                }
                                            }
                                        },
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
        [FromQuery(Name = "manufacturerId")] Guid? manufacturerId,
        [FromQuery(Name = "lastId")] Guid? lastCarId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "status")] CarStatusEnum? status,
        [FromQuery(Name = "keyword")] string? keyword = "",
        [FromQuery(Name = "limit")] int? limit = 10
    )
    {
        Result<OffsetPaginatedResponse<GetPersonalCars.Response>> result = await sender.Send(
            new GetPersonalCars.Query(
                ManufacturerId: manufacturerId,
                Amenities: amenities,
                FuelTypes: fuel,
                TransmissionTypes: transmission,
                LastCarId: lastCarId,
                Limit: limit!.Value,
                Status: status,
                Keyword: keyword
            )
        );
        return result.MapResult();
    }
}
