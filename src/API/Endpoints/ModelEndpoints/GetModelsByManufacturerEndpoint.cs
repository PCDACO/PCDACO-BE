using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetModelsByManufacturerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manufacturers/{id:guid}/models", Handle)
            .WithSummary("Get all models of a manufacturer")
            .WithTags("Models")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of car models for a specific manufacturer.

                    Access Control:
                    - Requires authentication

                    Features:
                    - Filter by manufacturer ID
                    - Additional filtering by model name (keyword)
                    - Pagination support
                    - Results ordered by creation date (newest first)
                    - Includes basic manufacturer details with each model

                    Query Parameters:
                    - index: Page number (default: 1)
                    - size: Number of items per page (default: 10)
                    - keyword: Optional search term to filter by model name
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns paginated list of models",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["name"] = new OpenApiString("Camry"),
                                                    ["releaseDate"] = new OpenApiString(
                                                        "2023-01-01T00:00:00Z"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2023-05-15T10:30:00Z"
                                                    ),
                                                    ["manufacturer"] = new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174001"
                                                        ),
                                                        ["name"] = new OpenApiString("Toyota"),
                                                    },
                                                },
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "223e4567-e89b-12d3-a456-426614174002"
                                                    ),
                                                    ["name"] = new OpenApiString("Corolla"),
                                                    ["releaseDate"] = new OpenApiString(
                                                        "2022-06-15T00:00:00Z"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2023-04-20T14:45:00Z"
                                                    ),
                                                    ["manufacturer"] = new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174001"
                                                        ),
                                                        ["name"] = new OpenApiString("Toyota"),
                                                    },
                                                },
                                            },
                                            ["totalItems"] = new OpenApiInteger(42),
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["hasNext"] = new OpenApiBoolean(true),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy danh sách mô hình xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - Manufacturer doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy hãng xe"),
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
        [FromRoute] Guid id,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? name = ""
    )
    {
        Result<OffsetPaginatedResponse<GetModelsByManufacturer.Response>> result =
            await sender.Send(
                new GetModelsByManufacturer.Query(
                    ManufacturerId: id!,
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Name: name!
                )
            );
        return result.MapResult();
    }
}
