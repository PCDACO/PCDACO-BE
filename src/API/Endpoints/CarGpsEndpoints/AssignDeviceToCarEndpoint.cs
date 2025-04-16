using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
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
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Assign a GPS device to a car with location data.

                    Process:
                    1. Validates car exists and is not deleted
                    2. Verifies car has an in-progress inspection schedule
                    3. Checks if device with OSBuildId exists:
                       - If exists and available: Updates status to InUsed
                       - If exists but not available: Returns error
                       - If doesn't exist: Creates a new device
                    4. Checks CarGPS association:
                       - If exists: Returns error that car already has a GPS device
                       - If doesn't exist: Creates new association

                    Requirements:
                    - Car must exist and not be deleted
                    - Car must have an in-progress inspection schedule
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
                        ["400"] = new()
                        {
                            Description = "Bad Request - Car already has GPS",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Xe đã được gắn thiết bị GPS"),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad Request - Car does not have an in-progress inspection schedule",
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
                                                "Xe chưa có lịch kiểm định nào đang được tiến hành, không thể gán thiết bị GPS !"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - GPS device not available",
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
                                                "Thiết bị GPS đã được sử dụng, vui lòng gỡ thiết bị trước khi gán vào xe mới"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, AssignDeviceToCarRequest request)
    {
        Result<AssignDeviceToCar.Response> result = await sender.Send(
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
