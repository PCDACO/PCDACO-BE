using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public sealed class GetAmenitiesOfCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/{id:guid}/amenities", Handle)
            .WithSummary("Get amenities of a specific car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of amenities associated with a specific car.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can view their car's amenities

                    Pagination:
                    - Supports offset-based pagination
                    - Default page size: 10 items
                    - Default page number: 1

                    Filtering:
                    - Optional keyword search
                    - Returns amenities matching the search criteria

                    Response includes:
                    - Amenity ID
                    - Name
                    - Description
                    - Creation timestamp
                    - Pagination metadata
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
                                                ["name"] = new OpenApiString("GPS Navigation"),
                                                ["description"] = new OpenApiString(
                                                    "Built-in GPS navigation system"
                                                ),
                                                ["createdAt"] = new OpenApiString(
                                                    "2024-03-15T10:00:00Z"
                                                )
                                            }
                                        },
                                        ["pageNumber"] = new OpenApiInteger(1),
                                        ["pageSize"] = new OpenApiInteger(10),
                                        ["totalItems"] = new OpenApiInteger(1),
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
                                "Forbidden - User not authorized to view this car's amenities",
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
                                        ["message"] = new OpenApiString("Không tìm thấy xe")
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
        Guid id,
        [AsParameters] GetAmenitiesOfCarRequest request
    )
    {
        Result<OffsetPaginatedResponse<GetAmenitiesOfCar.Response>> result = await sender.Send(
            new GetAmenitiesOfCar.Query(
                id,
                request.pageNumber!.Value,
                request.pageSize!.Value,
                request.keyword!
            )
        );
        return result.MapResult();
    }

    private record GetAmenitiesOfCarRequest(
        string? keyword,
        int? pageNumber = 1,
        int? pageSize = 10
    );
}
