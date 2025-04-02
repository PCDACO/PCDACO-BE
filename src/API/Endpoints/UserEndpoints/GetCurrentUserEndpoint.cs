using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/current", Handle)
            .WithSummary("Get current user information")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves information about the currently authenticated user.

                    Features:
                    - Returns complete user profile including personal information
                    - Decrypts sensitive information like phone numbers
                    - Includes statistics based on user role:
                      * For drivers: total completed rentals
                      * For owners: total cars, total times cars were rented
                    - Shows current account balance

                    The response includes all necessary user details for profile display and statistics.
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
                                            ["totalRent"] = new OpenApiInteger(15),
                                            ["totalRented"] = new OpenApiInteger(0),
                                            ["balance"] = new OpenApiFloat(1500000),
                                            ["totalCar"] = new OpenApiInteger(0),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy thông tin người dùng thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new()
                        {
                            Description = "Unauthorized - User not authenticated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Bạn chưa đăng nhập"),
                                    },
                                },
                            },
                        },
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

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetCurrentUser.Response> result = await sender.Send(new GetCurrentUser.Query());
        return result.MapResult();
    }
}
