using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetUserByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}", Handle)
            .WithSummary("Get user by ID")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves detailed information about a specific user by their ID.

                    Features:
                    - Returns user's personal information including name, email, and profile details
                    - Decrypts sensitive information like phone numbers
                    - Requires authentication to access

                    This endpoint is useful for displaying user profile information or verifying 
                    user details for administrative purposes.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns user information",
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
                                            ["name"] = new OpenApiString("Nguyễn Văn A"),
                                            ["email"] = new OpenApiString("nguyenvana@example.com"),
                                            ["avatarUrl"] = new OpenApiString(
                                                "https://example.com/avatar.jpg"
                                            ),
                                            ["address"] = new OpenApiString(
                                                "123 Đường ABC, Quận 1, TP.HCM"
                                            ),
                                            ["dateOfBirth"] = new OpenApiString(
                                                "1990-01-01T00:00:00Z"
                                            ),
                                            ["phone"] = new OpenApiString("0987654321"),
                                            ["role"] = new OpenApiString("Driver"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - User doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy thông tin người dùng"
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
        Result<GetUserById.Response> result = await sender.Send(new GetUserById.Query(id));
        return result.MapResult();
    }
}
