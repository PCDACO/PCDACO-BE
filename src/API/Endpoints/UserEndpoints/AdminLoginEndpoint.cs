using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class AdminLoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/admin/login", Handle)
            .WithSummary("Admin login")
            .WithTags("Auth")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Authenticate an admin user with their email and password.

                    This endpoint:
                    - Validates the admin's credentials
                    - Generates a new access token and refresh token
                    - Returns the tokens for authentication

                    Notes:
                    - Access tokens are used for API authorization
                    - Refresh tokens can be used to obtain new access tokens
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - User authenticated successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["accessToken"] = new OpenApiString(
                                                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                                            ),
                                            ["refreshToken"] = new OpenApiString(
                                                "6fd8d272-3f09-4a89-9157-9471d953d1b7"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Đăng nhập thành công"),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - User not found or invalid credentials",
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

    private async Task<IResult> Handle(ISender sender, LoginAdminRequest request)
    {
        Result<AdminLogin.Response> result = await sender.Send(
            new AdminLogin.Command(request.Email, request.Password)
        );
        return result.MapResult();
    }

    private record LoginAdminRequest(string Email, string Password);
}
