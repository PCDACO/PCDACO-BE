using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Contract.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class UpdateContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/contracts/update-from-schedule/{id:guid}", Handle)
            .WithSummary("Update car contract from inspection schedule")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Updates a car contract based on inspection schedule information.

                    Process:
                    1. Finds the inspection schedule by ID
                    2. Updates the associated contract with:
                       - Technician ID (current user)
                       - Sets status to Pending
                    3. Creates a new contract if one doesn't exist

                    Requirements:
                    - Must be executed by a technician
                    - Must be the technician assigned to the inspection
                    - Inspection schedule must exist and be in InProgress status
                    - Car must have a GPS device assigned

                    Returns:
                    - ContractId for further operations
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Contract updated successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật hợp đồng thành công"
                                        ),
                                        ["value"] = new OpenApiObject
                                        {
                                            ["contractId"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Car missing GPS device",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Xe chưa được gán thiết bị GPS"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not a technician or not assigned to the inspection",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không phải là kiểm định viên được chỉ định"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Inspection schedule not found",
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
                        ["409"] = new()
                        {
                            Description = "Conflict - Schedule is not in InProgress status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Lịch kiểm định không ở trạng thái đang diễn ra"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new UpdateContract.Command(id));
        return result.MapResult();
    }
}
