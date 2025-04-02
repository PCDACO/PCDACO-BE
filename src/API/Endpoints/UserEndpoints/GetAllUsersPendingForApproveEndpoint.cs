using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetAllUsersPendingForApproveEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/license/approve", Handle)
            .WithSummary("Get all users pending license approval filter by name or email of user")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves a paginated list of users who have uploaded license information but are waiting for approval.

                    Features:
                    - Returns driver and owner users whose license is pending approval
                    - Filters users by name or email using the keyword parameter
                    - Decrypts sensitive information like phone numbers and license details
                    - Paginates results with customizable page size and page number
                    - Requires administrator privileges to access

                    The response includes full user details necessary for license verification.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description =
                                "Success - Returns paginated list of users pending license approval",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["name"] = new OpenApiString("Nguyễn Văn A"),
                                                    ["email"] = new OpenApiString(
                                                        "nguyenvana@example.com"
                                                    ),
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
                                                    ["licenseNumber"] = new OpenApiString(
                                                        "123456789"
                                                    ),
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
                                            },
                                            ["totalItems"] = new OpenApiInteger(50),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["hasNext"] = new OpenApiBoolean(true),
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
                    },
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllUsersPendingForApprove.Response>> result =
            await sender.Send(
                new GetAllUsersPendingForApprove.Query(pageNumber!.Value, pageSize!.Value, keyword!)
            );
        return result.MapResult();
    }
}
