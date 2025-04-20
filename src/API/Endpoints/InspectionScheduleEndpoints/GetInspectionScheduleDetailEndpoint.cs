using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInspectionScheduleDetailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/{id:guid}", Handle)
            .WithSummary("Get inspection schedules detail by id")
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve detailed information about a specific inspection schedule.

                    Response includes:
                    - Basic schedule details (date, address, notes)
                    - Technician information
                    - Car owner information with decrypted phone number
                    - Car details and specifications
                    - Amenities associated with the car
                    - Contract ID and GPS device status

                    Notes:
                    - All users with valid authentication can access this endpoint
                    - Sensitive information like phone numbers are automatically decrypted
                    - Created timestamp is extracted from the UUID
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns inspection schedule details",
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
                                            ["date"] = new OpenApiString("2023-12-25T09:00:00Z"),
                                            ["address"] = new OpenApiString(
                                                "123 Đường Lê Lợi, Quận 1, TP.HCM"
                                            ),
                                            ["notes"] = new OpenApiString("Kiểm định định kỳ"),
                                            ["technician"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa7"
                                                ),
                                                ["name"] = new OpenApiString("Nguyễn Văn Kỹ Thuật"),
                                            },
                                            ["owner"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa8"
                                                ),
                                                ["name"] = new OpenApiString("Trần Văn Chủ"),
                                                ["avatarUrl"] = new OpenApiString(
                                                    "https://example.com/avatar.jpg"
                                                ),
                                                ["phone"] = new OpenApiString("0901234567"),
                                            },
                                            ["car"] = new OpenApiObject
                                            {
                                                ["id"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afa9"
                                                ),
                                                ["modelId"] = new OpenApiString(
                                                    "3fa85f64-5717-4562-b3fc-2c963f66afb0"
                                                ),
                                                ["modelName"] = new OpenApiString("Honda Civic"),
                                                ["fuelType"] = new OpenApiString("Xăng"),
                                                ["transmissionType"] = new OpenApiString("Tự động"),
                                                ["amenities"] = new OpenApiArray
                                                {
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "3fa85f64-5717-4562-b3fc-2c963f66afb1"
                                                        ),
                                                        ["name"] = new OpenApiString("Định vị GPS"),
                                                        ["iconUrl"] = new OpenApiString(
                                                            "https://example.com/icons/gps.png"
                                                        ),
                                                    },
                                                    new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "3fa85f64-5717-4562-b3fc-2c963f66afb2"
                                                        ),
                                                        ["name"] = new OpenApiString("Camera lùi"),
                                                        ["iconUrl"] = new OpenApiString(
                                                            "https://example.com/icons/camera.png"
                                                        ),
                                                    },
                                                },
                                            },
                                            ["createdAt"] = new OpenApiString(
                                                "2023-12-20T08:30:00Z"
                                            ),
                                            ["contractId"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afb3"
                                            ),
                                            ["hasGPSDevice"] = new OpenApiBoolean(true),
                                            ["type"] = new OpenApiString("NewCar"),
                                            ["status"] = new OpenApiString("InProgress"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - Inspection schedule doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy lịch kiểm định"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, CancellationToken cancellationToken)
    {
        Result<GetInspectionScheduleDetail.Response> result = await sender.Send(
            new GetInspectionScheduleDetail.Query(id),
            cancellationToken
        );
        return result.MapResult();
    }
}
