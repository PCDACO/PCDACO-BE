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

public sealed class GetAllOwnersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/owners", Handle)
            .WithSummary("Get all owners")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a paginated list of all car owners in the system.

                    Features:
                    - Pagination with configurable page size and number
                    - Search by name or email using keyword parameter
                    - Decrypted sensitive information (phone numbers)
                    - License approval status and ban status information
                    - Only accessible by administrators

                    Note: Sensitive information (phone numbers) is decrypted for authorized users
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns paginated list of car owners",
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
                                                        Guid.NewGuid().ToString()
                                                    ),
                                                    ["name"] = new OpenApiString("Nguyễn Văn A"),
                                                    ["email"] = new OpenApiString(
                                                        "owner1@example.com"
                                                    ),
                                                    ["avatarUrl"] = new OpenApiString(
                                                        "https://example.com/avatar.jpg"
                                                    ),
                                                    ["address"] = new OpenApiString(
                                                        "123 Đường Lê Lợi, Quận 1, TP.HCM"
                                                    ),
                                                    ["dateOfBirth"] = new OpenApiString(
                                                        "1990-01-15T00:00:00Z"
                                                    ),
                                                    ["phone"] = new OpenApiString("0901234567"),
                                                    ["role"] = new OpenApiString("Owner"),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2023-10-15T00:00:00Z"
                                                    ),
                                                    ["isApprovedLicense"] = new OpenApiBoolean(
                                                        true
                                                    ),
                                                    ["isBanned"] = new OpenApiBoolean(false),
                                                },
                                            },
                                            ["totalItems"] = new OpenApiInteger(50),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["hasNext"] = new OpenApiBoolean(true),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy danh sách chủ xe thành công"
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

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllOwners.Response>> result = await sender.Send(
            new GetAllOwners.Query(
                PageNumber: pageNumber!.Value,
                PageSize: pageSize!.Value,
                Keyword: keyword!
            )
        );
        return result.MapResult();
    }
}
