using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Model.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class CreateModelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/models", Handle)
            .WithSummary("Create a model")
            .WithTags("Models")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new car model in the system.

                    Access Control:
                    - Requires authentication
                    - Only available to users with Admin role

                    Features:
                    - Links model to existing manufacturer
                    - Validates model name uniqueness within manufacturer
                    - Idempotency support to prevent duplicate submissions

                    Validation Rules:
                    - Name: required, max 100 characters
                    - ReleaseDate: required, must be in the past or present
                    - ManufacturerId: required, must reference existing manufacturer
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Model successfully created",
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
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Tạo mô hình xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Manufacturer doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Hãng xe không tồn tại"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad Request - Model already exists for this manufacturer",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Mô hình xe đã tồn tại trong hãng xe"
                                        ),
                                    },
                                },
                            },
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
                                            "Bạn không có quyền thực hiện chức năng này !"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, CreateModelRequest request)
    {
        Result<CreateModel.Response> result = await sender.Send(
            new CreateModel.Command(request.Name, request.ReleaseDate, request.ManufacturerId)
        );
        return result.MapResult();
    }

    private record CreateModelRequest(string Name, DateTimeOffset ReleaseDate, Guid ManufacturerId);
}
