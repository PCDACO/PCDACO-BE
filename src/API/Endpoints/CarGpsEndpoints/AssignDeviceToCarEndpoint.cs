using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarGpsEndpoints;

public class AssignDeviceToCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/assign-device", Handle)
            .WithSummary("Assign Device To Car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Assign a GPS device to a car with location data.

                    Process:
                    1. Validates car exists and is not deleted
                    2. Checks if device with OSBuildId exists:
                       - If exists and available: Updates status to InUsed
                       - If exists but not available: Returns error
                       - If doesn't exist: Creates a new device
                    3. Checks CarGPS association:
                       - If exists and not deleted: Returns error
                       - If exists but deleted: Restores and updates location
                       - If doesn't exist: Creates new association

                    Requirements:
                    - Car must exist and not be deleted
                    - If device exists, it must be in Available status
                    - Car must not have an active GPS device association
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - GPS device assigned successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Tạo mới thành công"),
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
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Không tìm thấy xe"),
                                        },
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Device not available or already assigned",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Thiết bị GPS không khả dụng"),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Device already assigned to this car",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Thiết bị GPS này đang được sử dụng"),
                                        },
                                    },
                                },
                            },
                        },
                    },
                }
            );
        ;
    }

    private async Task<IResult> Handle(ISender sender, Guid id, AssignDeviceToCarRequest request)
    {
        Result result = await sender.Send(
            new AssignDeviceToCar.Command(
                CarId: id,
                Longtitude: request.Longtitude!.Value,
                Latitude: request.Latitude!.Value,
                OSBuildId: request.OSBuildId,
                DeviceName: request.DeviceName
            )
        );
        return result.MapResult();
    }

    private record AssignDeviceToCarRequest(
        string OSBuildId,
        string DeviceName,
        double? Longtitude,
        double? Latitude
    );
}
