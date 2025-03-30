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
                    - Must be executed by the technician assigned to the inspection
                    - Inspection schedule must exist

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
                        ["403"] = new()
                        {
                            Description = "Forbidden - Unauthorized to update this contract",
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
                                                "Bạn không phải là kiểm định viên được chỉ định"
                                            ),
                                        },
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
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Không tìm thấy lịch kiểm định"),
                                        },
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
