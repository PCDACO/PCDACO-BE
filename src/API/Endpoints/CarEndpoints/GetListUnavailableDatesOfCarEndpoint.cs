using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetListUnavailableDatesOfCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/{id:guid}/unavailable-dates", Handle)
            .WithSummary("Get list of unavailable dates for a car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve a list of dates when a specific car is marked as unavailable.

                    Access Control:
                    - Requires authentication
                    - Available to all authenticated users

                    Usage:
                    - Used for displaying blocked dates in booking calendars
                    - Shows dates explicitly marked as unavailable by the car owner
                    - Does not include dates that are unavailable due to existing bookings
                    - Can filter results by month and year

                    Parameters:
                    - id: Car ID
                    - month (optional): Filter by month (1-12)
                    - year (optional): Filter by year (defaults to current year if only month is specified)

                    Response:
                    - List of dates when the car is marked as unavailable
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
                                        ["value"] = new OpenApiArray
                                        {
                                            new OpenApiObject
                                            {
                                                ["date"] = new OpenApiString(
                                                    "2024-07-15T00:00:00Z"
                                                ),
                                                ["isAvailable"] = new OpenApiBoolean(false),
                                            },
                                            new OpenApiObject
                                            {
                                                ["date"] = new OpenApiString(
                                                    "2024-07-16T00:00:00Z"
                                                ),
                                                ["isAvailable"] = new OpenApiBoolean(false),
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid parameters",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Tháng không hợp lệ"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
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
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null
    )
    {
        Result<List<GetListUnavailableDatesOfCar.Response>> result = await sender.Send(
            new GetListUnavailableDatesOfCar.Query(id, month, year)
        );

        return result.MapResult();
    }
}
