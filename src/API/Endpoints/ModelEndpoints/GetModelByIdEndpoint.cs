using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetModelByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models/{id:guid}", Handle)
            .WithSummary("Get model by ID")
            .WithTags("Models")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific car model.

                    Details Included:
                    - Model ID and name
                    - Release date
                    - Creation timestamp
                    - Associated manufacturer information

                    Access Control:
                    - Requires authentication
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns model details",
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
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy thông tin mô hình xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - Model doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy mô hình xe"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetModelById.Response> result = await sender.Send(new GetModelById.Query(id));
        return result.MapResult();
    }
}
