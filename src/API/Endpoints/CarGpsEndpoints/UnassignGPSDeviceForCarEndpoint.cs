using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarGpsEndpoints;

public class UnassignGPSDeviceForCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/cars/devices/{id:guid}/unassign-gps-device", Handle)
            .WithSummary("Unassign GPS device from car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Unassign a GPS device from its associated car.

                    Process:
                    1. Validates GPS device exists and is not deleted
                    2. Finds the car associated with this GPS device
                    3. Verifies if car is in Pending status or is deleted
                    4. Updates the device status to Available
                    5. Removes the association between car and device

                    Requirements:
                    - GPS device must exist and be associated with a car
                    - Car must be in Pending status or already deleted

                    This operation is useful for:
                    - Removing devices from deleted cars
                    - Reassigning devices from non-active cars to other vehicles
                    - Maintaining device inventory
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - GPS device unassigned successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Gỡ thiết bị GPS khỏi xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description =
                                "Not Found - GPS device not found or not associated with any car",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString(
                                                "Thiết bị GPS không được gán cho xe nào"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Car is not in Pending status or deleted",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString(
                                                "Chỉ có thể gỡ thiết bị GPS khỏi xe đã bị xóa hoặc đang trong trạng thái chờ"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - GPS device not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Không tìm thấy thiết bị GPS"),
                                        },
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new UnassignGPSDeviceForCar.Command(id));
        return result.MapResult();
    }
}
