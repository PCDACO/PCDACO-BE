using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class SignUpEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signup", Handle)
            .WithSummary("Sign up a new user")
            .WithTags("Auth")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Register a new user account in the system.

                    Features:
                    - Creates a new user account with basic information
                    - Assigns the user to the specified role (defaults to Driver)
                    - Validates uniqueness of email and phone number
                    - Securely encrypts sensitive personal information
                    - Generates authentication tokens for immediate login
                    - Creates user statistics tracking

                    Validation Rules:
                    - Name: 3-50 characters
                    - Email: Valid format and must be unique
                    - Password: At least 6 characters
                    - Address: Required
                    - Date of Birth: Must be in the past
                    - Phone: Required and must be unique
                    - Role: Must be an existing role (Admin role is restricted)

                    Notes:
                    - The account is active immediately after creation
                    - Access token and refresh token are provided for authentication
                    - The refresh token has a 24-hour validity period
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - User registered successfully",
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
                                                "c9e44fcb-8be0-4c7a-8277-b410a23c7d2a"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Đăng ký thành công"),
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
                                        ["message"] = new OpenApiString("Validation failed"),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Email đã tồn tại"),
                                            new OpenApiString("Số điện thoại đã tồn tại"),
                                            new OpenApiString("Mật khẩu phải có ít nhất 6 ký tự"),
                                        },
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, SignUpRequest request)
    {
        Result<SignUp.Response> result = await sender.Send(
            new SignUp.Command(
                request.Name,
                request.Email,
                request.Password,
                request.Address,
                request.DateOfBirth,
                request.Phone,
                request.RoleName!
            )
        );
        return result.MapResult();
    }

    private record SignUpRequest(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string? RoleName = "Driver"
    );
}
