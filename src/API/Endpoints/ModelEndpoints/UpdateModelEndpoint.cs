using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Model.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class UpdateModelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/models/{id}", Handle)
            .WithSummary("Update a model")
            .WithTags("Models")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Update details of an existing car model.

                    Access Control:
                    - Requires authentication
                    - Only available to users with Admin role

                    Features:
                    - Updates model name, release date, and manufacturer
                    - Performs validation of all fields
                    - Verifies manufacturer exists

                    Validation Rules:
                    - Name: required, max 100 characters
                    - ReleaseDate: required
                    - ManufacturerId: required, must reference existing manufacturer
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Model successfully updated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["modelId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["name"] = new OpenApiString("Updated Model Name"),
                                            ["releaseDate"] = new OpenApiString(
                                                "2023-01-01T00:00:00Z"
                                            ),
                                            ["manufacturerId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật mô hình xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Tên mô hình xe không được để trống"),
                                            new OpenApiString(
                                                "Tên mô hình xe không được vượt quá 100 ký tự"
                                            ),
                                            new OpenApiString("Ngày phát hành không được để trống"),
                                            new OpenApiString("hãng xe không được để trống"),
                                        },
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
                        ["404"] = new()
                        {
                            Description = "Not Found - Model or manufacturer doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Mô hình xe không tồn tại"),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateModelRequest request)
    {
        Result<UpdateModel.Response> result = await sender.Send(
            new UpdateModel.Command(id, request.Name, request.ReleaseDate, request.ManufacturerId)
        );
        return result.MapResult();
    }

    private record UpdateModelRequest(string Name, DateTimeOffset ReleaseDate, Guid ManufacturerId);
}
