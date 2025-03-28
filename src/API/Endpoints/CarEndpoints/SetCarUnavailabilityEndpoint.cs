using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class SetCarUnavailabilityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/availability", Handle)
            .WithSummary("Set car unavailability for specific dates")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Set a car as unavailable for specific dates.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can modify their car's availability

                    Usage:
                    - Allows car owners to block multiple dates when the car shouldn't be available
                    - Creates or updates CarAvailability records for the specified dates
                    - Affects the car's availability in search results

                    Notes:
                    - Setting IsAvailable=false blocks the dates from being booked
                    - Setting IsAvailable=true makes the dates available again (default availability)
                    - Multiple dates can be processed in a single request
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid dates",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Ngày không hợp lệ"),
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
                                            "Bạn không có quyền thực hiện thao tác này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car doesn't exist",
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
                            Description = "Conflict - Existing bookings on one or more dates",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể thay đổi trạng thái vì ngày {{date}} đã có đơn đặt xe"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, SetCarUnavailabilityRequest request)
    {
        Result result = await sender.Send(
            new SetCarUnavailability.Command(
                CarId: id,
                Dates: request.Dates,
                IsAvailable: request.IsAvailable
            )
        );
        return result.MapResult();
    }

    private record SetCarUnavailabilityRequest(
        List<DateTimeOffset> Dates,
        bool IsAvailable = false
    );
}
