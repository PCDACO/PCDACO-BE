using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class UpdateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/{id:guid}", Handle)
            .WithSummary("Update user profile's information")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Updates a user's profile information.

                    Features:
                    - Updates personal details including name, email, address
                    - Updates date of birth and phone number
                    - Requires authentication and proper authorization
                    - Encrypts sensitive information like phone numbers

                    Notes:
                    - Users can only update their own profiles
                    - All fields must be provided
                    - Field validations are enforced
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - User information updated successfully",
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
                                        ["message"] = new OpenApiString("Cập nhật thành công"),
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
                                            new OpenApiString("Tên không được để trống"),
                                            new OpenApiString("Email không hợp lệ"),
                                            new OpenApiString("Số điện thoại không được để trống"),
                                            new OpenApiString(
                                                "Ngày sinh phải nhỏ hơn ngày hiện tại"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User trying to update another user's profile",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện hành động này"
                                        ),
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

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateUserRequest request)
    {
        Result<UpdateUser.Response> result = await sender.Send(
            new UpdateUser.Command(
                id,
                request.Name,
                request.Email,
                request.Address,
                request.DateOfBirth,
                request.Phone
            )
        );
        return result.MapResult();
    }

    private record UpdateUserRequest(
        string Name,
        string Email,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone
    );
}
