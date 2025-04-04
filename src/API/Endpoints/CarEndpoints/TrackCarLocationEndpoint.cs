using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class TrackCarLocationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/track", Handle)
            .WithSummary("Track car location from GPS device")
            .WithTags("Cars")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Updates the real-time location of a car from its GPS device.

                    Notes:
                    - Updates the car's current location in CarGPS table
                    - If there's an active booking for the car:
                      * Creates a new TripTracking record
                      * Calculates distance from previous tracking point
                      * Updates cumulative distance for the trip
                    - Location data uses WGS84 coordinate system (SRID: 4326)
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Location updated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật vị trí thành công"
                                        )
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid coordinates",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Tọa độ không hợp lệ")
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car or GPS device doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy xe hoặc thiết bị GPS"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        TrackCarLocationRequest request
    )
    {
        Result result = await sender.Send(
            new TrackCarLocation.Command(id, request.Latitude, request.Longitude)
        );
        return result.MapResult();
    }

    private sealed record TrackCarLocationRequest(decimal Latitude, decimal Longitude);
}
