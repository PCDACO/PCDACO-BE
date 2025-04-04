using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarGpsEndpoints;

public class SwitchGPSDeviceForCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/switch-gps-device", Handle)
            .WithSummary("Switch GPS device for a car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Switch a car's GPS device to another one.

                    Process:
                    1. Validates car exists and is not deleted
                    2. Validates GPS device exists and is not deleted
                    3. Checks if the GPS device is already assigned:
                       - If already assigned to another car: Changes that car's status to Pending 
                         and reassigns the device to the requested car
                       - If not already assigned: Creates a new association
                    4. Updates the device location with the provided coordinates

                    Requirements:
                    - Car must exist and not be deleted
                    - GPS device must exist and not be deleted

                    Note: This operation can transfer a GPS device from one car to another.
                    The previous car's status will be changed to Pending.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - GPS device switched successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["carId"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                            ),
                                            ["gpsDeviceId"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa7"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật thiết bị GPS cho xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car or GPS device not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Không tìm thấy xe"),
                                        },
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, SwitchGPSDeviceRequest request)
    {
        Result<SwitchGPSDeviceForCar.Response> result = await sender.Send(
            new SwitchGPSDeviceForCar.Command(
                CarId: id,
                GPSDeviceId: request.GPSDeviceId,
                Longtitude: request.Longitude,
                Latitude: request.Latitude
            )
        );
        return result.MapResult();
    }

    private record SwitchGPSDeviceRequest(Guid GPSDeviceId, double Longitude, double Latitude);
}
