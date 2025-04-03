using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class CreateAdminUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/admin", Handle)
            .WithSummary("Create an admin user")
            // .AddEndpointFilter<IdempotencyFilter>()
            .WithTags("Users")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Creates the default admin user account in the system.

                    Features:
                    - Creates a pre-configured admin account with fixed credentials
                    - Sets up encryption keys for secure data storage
                    - Assigns the admin role with all system privileges

                    Notes:
                    - This endpoint can only be called once during system initialization
                    - Default email is admin@gmail.com with password "admin"
                    - Subsequent calls will return a forbidden result
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Admin account created",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Tạo tài khoản admin thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Admin role not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể tạo tài khoản admin"
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
        Result result = await sender.Send(new CreateAdminUser.Command());
        return result.MapResult();
    }
}
