using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_Contract.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class SignCarContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/contract/sign", Handle)
            .WithSummary("Sign car inspection contract")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Sign a car inspection contract by owner or technician.

                    Requirements:
                    - Contract must exist and be in Pending status
                    - Related inspection schedule must exist and be in InProgress status
                    - Contract can only be signed by the car owner or the assigned technician

                    Process:
                    - If signed by owner: Updates OwnerSignatureDate and sets status to OwnerSigned
                    - If signed by technician: Updates TechnicianSignatureDate and sets status to TechnicianSigned
                    - If both have signed, inspection schedule status is updated to Signed

                    The system automatically detects the user role and applies the appropriate signature.
                    Once both signatures are present, the schedule status will be updated to Signed.

                    Access Control:
                    - Car owners can only sign their own car contracts
                    - Technicians can only sign contracts for inspections assigned to them
                    - Other roles are not permitted to sign contracts
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Contract signed successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["contractId"] = new OpenApiString(
                                                "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                            ),
                                            ["status"] = new OpenApiString("OwnerSigned"),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Hợp đồng đã được chủ xe ký thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to sign this contract",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền ký hợp đồng này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Contract or inspection schedule not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy hợp đồng"),
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Contract not in signable state",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Hợp đồng không ở trạng thái có thể ký"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        SignCarContractRequest request
    )
    {
        var result = await sender.Send(
            new SignCarContract.Command(CarId: id, Signature: request.Signature)
        );
        return result.MapResult();
    }

    private sealed record SignCarContractRequest(string Signature);
}
