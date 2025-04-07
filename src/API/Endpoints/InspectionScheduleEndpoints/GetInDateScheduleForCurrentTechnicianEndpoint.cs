using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInDateScheduleForCurrentTechnicianEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/technician", Handle)
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithSummary("Get inspection schedules for current technician by inspection date")
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve inspection schedules for the current technician for a specific date.

                    Features:
                    - Fetches all cars scheduled for inspection on the specified date
                    - If no date is provided, defaults to today's date
                    - Returns detailed car information including model, owner, and images
                    - License plate numbers are automatically decrypted for the technician

                    Response includes:
                    - Technician name
                    - Inspection date
                    - List of cars to be inspected with full details
                    - Car images and specifications
                    - Car owner information
                    - Inspection address

                    Notes:
                    - Only accessible to users with technician role
                    - Only returns schedules with Pending status
                    - Schedules are sorted by ID ascending
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description =
                                "Success - Returns inspection schedules for the technician",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["technicianName"] = new OpenApiString(
                                                "Nguyễn Văn Kỹ Thuật"
                                            ),
                                            ["inspectionDate"] = new OpenApiString(
                                                "2023-12-25T09:00:00Z"
                                            ),
                                            ["cars"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                                    ),
                                                    ["modelId"] = new OpenApiString(
                                                        "3fa85f64-5717-4562-b3fc-2c963f66afa7"
                                                    ),
                                                    ["inspectionScheduleId"] = new OpenApiString(
                                                        "3fa85f64-5717-4562-b3fc-2c963f66afa8"
                                                    ),
                                                    ["modelName"] = new OpenApiString(
                                                        "Honda Civic"
                                                    ),
                                                    ["manufacturerName"] = new OpenApiString(
                                                        "Honda"
                                                    ),
                                                    ["licensePlate"] = new OpenApiString(
                                                        "59A-12345"
                                                    ),
                                                    ["color"] = new OpenApiString("Đen"),
                                                    ["seat"] = new OpenApiInteger(5),
                                                    ["description"] = new OpenApiString(
                                                        "Xe sedan 5 chỗ"
                                                    ),
                                                    ["transmissionType"] = new OpenApiString(
                                                        "Tự động"
                                                    ),
                                                    ["fuelType"] = new OpenApiString("Xăng"),
                                                    ["fuelConsumption"] = new OpenApiDouble(7.5),
                                                    ["requiresCollateral"] = new OpenApiBoolean(
                                                        true
                                                    ),
                                                    ["price"] = new OpenApiDouble(800000),
                                                    ["images"] = new OpenApiArray
                                                    {
                                                        new OpenApiObject
                                                        {
                                                            ["id"] = new OpenApiString(
                                                                "3fa85f64-5717-4562-b3fc-2c963f66afa9"
                                                            ),
                                                            ["url"] = new OpenApiString(
                                                                "https://example.com/image1.jpg"
                                                            ),
                                                            ["imageTypeName"] = new OpenApiString(
                                                                "Exterior"
                                                            ),
                                                        },
                                                    },
                                                    ["owner"] = new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "3fa85f64-5717-4562-b3fc-2c963f66afb0"
                                                        ),
                                                        ["name"] = new OpenApiString(
                                                            "Trần Văn Chủ"
                                                        ),
                                                        ["avatarUrl"] = new OpenApiString(
                                                            "https://example.com/avatar.jpg"
                                                        ),
                                                    },
                                                    ["inspectionAddress"] = new OpenApiString(
                                                        "123 Đường Lê Lợi, Quận 1, TP.HCM"
                                                    ),
                                                },
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User does not have technician role",
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
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        DateTimeOffset? inspectionDate = null,
        bool? isIncident = null
    )
    {
        Result<GetInDateScheduleForCurrentTechnician.Response> result = await sender.Send(
            new GetInDateScheduleForCurrentTechnician.Query(
                InspectionDate: inspectionDate,
                IsIncident: isIncident
            )
        );
        return result.MapResult();
    }
}
