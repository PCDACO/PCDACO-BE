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

public class GetCarForStaffsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff/cars", Handle)
            .WithSummary("Get cars list for staff members")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of all cars in the system for staff members.

                    Access Control:
                    - Requires authentication
                    - Restricted to staff members

                    Features:
                    - Includes soft-deleted cars (IgnoreQueryFilters)
                    - Full car details with decrypted license plates
                    - Comprehensive related data inclusion

                    Pagination:
                    - Offset-based pagination
                    - Configurable page size and number

                    Filtering:
                    - Keyword search
                    - Status filtering
                    - Shows only available cars by default

                    Details Included:
                    - Car specifications (model, transmission, fuel type)
                    - Owner information
                    - Location details
                    - Images and amenities
                    - Manufacturer details
                    - Decrypted license plates for staff view
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
                                                ["status"] = new OpenApiString("Available"),
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
                            Description =
                                "Forbidden - User not authorized to access staff features",
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
        [FromQuery(Name = "status")] CarStatusEnum? status,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = "",
        [FromQuery(Name = "onlyNoGps")] bool? onlyNoGps = false
    )
    {
        Result<OffsetPaginatedResponse<GetCarsForStaffs.Response>> result = await sender.Send(
            new GetCarsForStaffs.Query(
                pageNumber,
                pageSize,
                keyword!,
                status,
                OnlyNoGps: onlyNoGps)
        );
        return result.MapResult();
    }
}