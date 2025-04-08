using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_GPSDevice.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class UpdateGPSDeviceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/gps-devices/{id:guid}", Handle)
            .WithSummary("Update GPS Device")
            .WithTags("GPS Devices")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Updates an existing GPS device's name and status.

                    Requirements:
                    - User must have admin role
                    - GPS device must exist and not be deleted

                    Process:
                    - Verifies user has admin permissions
                    - Finds the GPS device by ID
                    - Updates the device name and status
                    - Saves changes to the database

                    Note: 
                    - Only available to admin users
                    - Device status can be changed between Available, InUsed, and Maintenance
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Device updated successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["id"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not an admin",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện chức năng này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - GPS device doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy thiết bị GPS"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
        ;
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateGPSDeviceRequest request)
    {
        Result<UpdateGPSDevice.Response> result = await sender.Send(
            new UpdateGPSDevice.Command(id, request.Name, request.Status),
            default
        );
        return result.MapResult();
    }

    private record UpdateGPSDeviceRequest(string Name, DeviceStatusEnum Status);
}
