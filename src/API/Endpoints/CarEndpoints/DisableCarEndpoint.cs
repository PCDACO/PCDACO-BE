using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class DisableCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/disable", Handle)
            .WithSummary("Disable a car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Disable a car listing temporarily.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can disable their own car

                    Process:
                    - Changes car status from Available to Inactive
                    - Prevents the car from appearing in search results
                    - Can be re-enabled later by the owner

                    Requirements:
                    - Car must be in Available status
                    - Car must not have any ongoing bookings
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Car has been disabled",
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
                                            ["status"] = new OpenApiString("Inactive"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Xe đã được tạm dừng hoạt động thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Car cannot be disabled",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Xe phải đang ở trạng thái có sẵn mới có thể tạm dừng hoạt động"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not the car owner",
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
                            Description = "Not Found - Car not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe"),
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Car has active bookings",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Xe đang có lịch đặt, không thể tạm dừng hoạt động"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<DisableCar.Response> result = await sender.Send(new DisableCar.Command(id));
        return result.MapResult();
    }
}
