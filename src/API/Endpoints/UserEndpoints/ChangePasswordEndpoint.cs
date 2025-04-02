using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class ChangePasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/{id:guid}/password", Handle)
            .WithSummary("Change user's password")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Change a user's password by providing the old password and new password.

                    This endpoint:
                    - Verifies the user's current password
                    - Validates the new password meets requirements
                    - Updates the password if all checks pass

                    Notes:
                    - User can only change their own password
                    - New password must be at least 6 characters long
                    - New password cannot be the same as the old password
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Password changed successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["userId"] = new OpenApiString(
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
                            Description = "Bad Request - Invalid password or validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Mật khẩu cũ không đúng"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User trying to change another user's password",
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

    private async Task<IResult> Handle(ISender sender, Guid id, ChangePasswordRequest request)
    {
        Result<ChangePassword.Response> result = await sender.Send(
            new ChangePassword.Command(id, request.OldPassword, request.NewPassword)
        );
        return result.MapResult();
    }

    private record ChangePasswordRequest(string OldPassword, string NewPassword);
}
