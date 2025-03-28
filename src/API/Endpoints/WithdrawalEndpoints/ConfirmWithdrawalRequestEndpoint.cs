using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_Withdraw.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.WithdrawalEndpoints;

public class ConfirmWithdrawalRequestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/withdrawals/{id}/confirm", Handle)
            .WithSummary("Confirm a withdrawal request")
            .WithTags("Transactions")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Confirm a withdrawal request and process the transaction.

                    Access Control:
                    - Requires authentication
                    - Only admin users can confirm withdrawals

                    Confirmation Process:
                    - Validates withdrawal request status (must be Pending)
                    - Uploads transaction proof image to Cloudinary
                    - Creates transaction record
                    - Updates withdrawal request status to Completed
                    - Updates user's balance

                    Transaction Handling:
                    - Creates a completed transaction record
                    - Deducts amount from user's balance
                    - Links transaction to withdrawal request
                    - Stores proof image with format: Withdrawal-{id}-Proof
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
                                        ["value"] = new OpenApiObject
                                        {
                                            ["withdrawalId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["transactionId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174001"
                                            ),
                                            ["status"] = new OpenApiString("Completed"),
                                            ["transactionProofUrl"] = new OpenApiString(
                                                "https://cloudinary.com/withdrawal-123-proof.jpg"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Phải có ảnh chứng minh giao dịch"
                                        )
                                    }
                                }
                            }
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
                                            "Chỉ admin mới có quyền xác nhận giao dịch"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Withdrawal request doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy yêu cầu rút tiền"
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
        [FromRoute] Guid id,
        [FromForm] ConfirmWithdrawalRequestRequest request
    )
    {
        var result = await sender.Send(
            new ConfirmWithdrawalRequest.Command(
                id,
                request.TransactionProof.OpenReadStream(),
                request.AdminNote
            )
        );

        return result.MapResult();
    }

    public record ConfirmWithdrawalRequestRequest(
        IFormFile TransactionProof,
        string? AdminNote = null
    );
}
