using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetUserPendingForApprovalEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/approval", Handle)
            .WithSummary("Get User Need To Be Approve By Id")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves details about a specific user who has uploaded license information and is waiting for approval.

                    Features:
                    - Returns detailed user information including name, email, and profile data
                    - Shows license details including number, expiry date, and image URLs
                    - Decrypts sensitive information like phone numbers and license details
                    - Requires administrator privileges to access

                    This endpoint is used for the license approval process by administrators.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns user details pending approval",
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
                                            ["createdAt"] = new OpenApiString(
                                                "2023-10-15T00:00:00Z"
                                            ),
                                            ["licenseNumber"] = new OpenApiString("123456789"),
                                            ["licenseExpiryDate"] = new OpenApiString(
                                                "2025-10-15T00:00:00Z"
                                            ),
                                            ["licenseImageFrontUrl"] = new OpenApiString(
                                                "https://example.com/license-front.jpg"
                                            ),
                                            ["licenseImageBackUrl"] = new OpenApiString(
                                                "https://example.com/license-back.jpg"
                                            ),
                                            ["isApprovedLicense"] = new OpenApiNull(),
                                            ["licenseRejectReason"] = new OpenApiNull(),
                                            ["licenseApprovedAt"] = new OpenApiNull(),
                                            ["licenseImageUploadedAt"] = new OpenApiString(
                                                "2023-10-14T00:00:00Z"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy danh sách người lái xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not an administrator",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description =
                                "Not Found - User doesn't exist or doesn't have pending license",
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

    public async Task<IResult> Handle(Guid id, ISender sender)
    {
        Result<GetUserPendingForApproval.Response> result = await sender.Send(
            new GetUserPendingForApproval.Query(id)
        );
        return result.MapResult();
    }
}
