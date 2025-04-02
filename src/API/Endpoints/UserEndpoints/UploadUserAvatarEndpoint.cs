using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class UploadUserAvatarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/{id:guid}/avatar", Handle)
            .WithSummary("Upload user's avatar")
            .WithTags("Users")
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Uploads a new avatar image for the user.

                    Features:
                    - Updates the user's profile picture
                    - Stores image on Cloudinary with proper naming convention
                    - Returns the updated avatar URL

                    Notes:
                    - Users can only update their own avatars
                    - File size and type are validated
                    - Previous avatar is replaced automatically
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Avatar uploaded successfully",
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
                                            ["avatarUrl"] = new OpenApiString(
                                                "https://cloudinary.com/user-avatar.jpg"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật ảnh đại diện thành công"
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
                                            new OpenApiString("Ảnh đại diện không được để trống"),
                                        },
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User trying to update another user's avatar",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không có quyền cập nhật ảnh đại diện của người dùng khác"
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
                                        ["message"] = new OpenApiString("Người dùng không tồn tại"),
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
        Guid id,
        IFormFile avatar,
        CancellationToken cancellationToken
    )
    {
        Result<UploadUserAvatar.Response> result = await sender.Send(
            new UploadUserAvatar.Command(id, avatar.OpenReadStream()),
            cancellationToken
        );
        return result.MapResult();
    }
}
