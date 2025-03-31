using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class GetInProgressInspectionScheduleForCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inspection-schedules/in-progress", Handle)
            .WithSummary("Get current in-progress inspection schedules of current user(technician)")
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieves the current in-progress inspection schedule for the authenticated technician.

                    Returns:
                    - Inspection schedule ID and date
                    - Car owner name
                    - Inspection address
                    - Decrypted license plate of the vehicle

                    Notes:
                    - Only accessible to users with technician role
                    - Only returns schedules with InProgress status
                    - Returns at most one schedule that the technician is currently working on
                    - License plate is automatically decrypted for the technician
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns the in-progress inspection schedule",
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
                                            ["ownerName"] = new OpenApiString("Trần Văn Chủ"),
                                            ["address"] = new OpenApiString(
                                                "123 Đường Lê Lợi, Quận 1, TP.HCM"
                                            ),
                                            ["licensePlate"] = new OpenApiString("59A-12345"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công"),
                                    },
                                },
                            },
                        },
                        ["401"] = new()
                        {
                            Description = "Unauthorized - User not authenticated",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập"
                                        ),
                                    },
                                },
                            },
                        },
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
                        ["404"] = new()
                        {
                            Description = "Not Found - No in-progress inspection schedule found",
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

    private async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        Result<GetInProgressInspectionScheduleForCurrentUser.Response> result = await sender.Send(
            new GetInProgressInspectionScheduleForCurrentUser.Query(),
            cancellationToken
        );
        return result.MapResult();
    }
}
