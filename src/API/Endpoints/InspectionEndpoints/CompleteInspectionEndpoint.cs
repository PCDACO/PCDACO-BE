using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionEndpoints;

public class CompleteInspectionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/inspection-schedules/{id:guid}/complete", Handle)
            .WithSummary("Complete a car inspection")
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allow technician to complete a car inspection and generate inspection contract.

                    Inspection Process:
                    - Only assigned technician can complete their inspection
                    - Inspection must be in 'Pending' status
                    - Technician must provide inspection results
                    - A GPS device must be assigned to approved cars
                    - Contract will be generated with inspection results

                    Approval Process:
                    - If approved:
                        + Car status will be set to 'Available'
                        + GPS device will be marked as 'InUsed'
                        + Contract status will be set to 'Completed'
                    - If rejected:
                        + Car status will be set to 'Rejected'
                        + Contract status will be set to 'Rejected'
                        + GPS device remains 'Available'

                    Notes:
                    - Only technicians can complete inspections
                    - Inspection results are required
                    - GPS device must be available for approved cars
                    - Contract will include all inspection details and photos
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
                                        ["value"] = new OpenApiString(
                                            "Kiểm định hoàn tất, xe đã sẵn sàng cho thuê"
                                        ),
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid input or GPS device not available",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Thiết bị GPS này đã được sử dụng hoặc không khả dụng"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not a technician or not assigned to this inspection",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không phải là kiểm định viên được chỉ định"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description =
                                "Not Found - Inspection schedule or GPS device doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy lịch kiểm định"
                                        )
                                    }
                                }
                            }
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Contract is not in correct status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Hợp đồng không ở trạng thái chờ kiểm định"
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
        CompleteInspectionRequest request
    )
    {
        var result = await sender.Send(
            new CompleteInspection.Command(
                id,
                request.InspectionResults,
                request.GPSDeviceId,
                request.IsApproved
            )
        );
        return result.MapResult();
    }

    private sealed record CompleteInspectionRequest(
        string InspectionResults,
        Guid GPSDeviceId,
        bool IsApproved
    );
}
