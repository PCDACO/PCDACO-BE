using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class CreateStaffEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/staff", Handle)
            .WithSummary("Create a staff user (consultant or technician)")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new staff member with either consultant or technician role.

                    Creates:
                    - User account with encrypted sensitive information
                    - Assigns appropriate role permissions
                    - Generates encryption keys for data protection

                    Notes:
                    - Only administrators can create staff accounts
                    - Requires validation of all user information
                    - Email and phone must be unique in the system
                    - Password must be at least 6 characters long
                    - Valid roles are 'consultant' or 'technician' only
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Staff user successfully created",
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
                                        ["message"] = new OpenApiString("Tạo mới thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Email or phone number already in use",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Email đã tồn tại trong hệ thống"
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
                                            "Bạn không có quyền thực hiện thao tác này"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, CreateStaffRequest request)
    {
        Result<CreateStaff.Response> result = await sender.Send(
            new CreateStaff.Command(
                request.Name,
                request.Email,
                request.Password,
                request.Address,
                request.DateOfBirth,
                request.Phone,
                request.RoleName
            )
        );
        return result.MapResult();
    }

    private record CreateStaffRequest(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string RoleName
    );
}
