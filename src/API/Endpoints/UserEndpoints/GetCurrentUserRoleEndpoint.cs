using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public sealed class GetCurrentUserRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/role", Handle)
            .WithSummary("Get current user role")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves the role of the currently authenticated user.

                    Features:
                    - Returns the user's role name (Admin, Driver, Owner, etc.)
                    - Requires authentication
                    - Simple endpoint for quick role checks

                    This endpoint is useful for UI components that need to display or make decisions
                    based on the current user's role.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns the user's role",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["role"] = new OpenApiString("Driver"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy thông tin vai trò người dùng thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetCurrentUserRole.Response> result = await sender.Send(
            new GetCurrentUserRole.Query()
        );
        return result.MapResult();
    }
}
