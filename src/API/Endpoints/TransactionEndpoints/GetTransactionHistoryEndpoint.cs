using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Transaction.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransactionEndpoints;

public class GetTransactionHistoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", Handle)
            .WithSummary("Get transaction history")
            .WithDescription(
                "Get paginated list of user's transactions with offset-based pagination"
            )
            .WithTags("Transactions")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve paginated list of user's transaction history with offset-based pagination.

                    Access Control:
                    - Users can only view their own transactions (where they are sender or receiver)

                    Filtering Options:
                    - Search: Filter by transaction description or type
                    - Type: Filter by transaction type
                    - Date Range: Filter by date range (fromDate to toDate)

                    Pagination:
                    - Offset-based pagination using pageNumber and pageSize
                    - Default pageSize: 10 items per page
                    - Returns hasNext flag for additional pages
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
                                            ["items"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["type"] = new OpenApiString("DEPOSIT"),
                                                    ["amount"] = new OpenApiDouble(2000000),
                                                    ["balanceAfter"] = new OpenApiDouble(5000000),
                                                    ["description"] = new OpenApiString(
                                                        "Nạp tiền vào ví"
                                                    ),
                                                    ["createdAt"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["status"] = new OpenApiString("SUCCESS"),
                                                    ["details"] = new OpenApiObject
                                                    {
                                                        ["bookingId"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174000"
                                                        ),
                                                        ["bankName"] = new OpenApiString(
                                                            "VietcomBank"
                                                        ),
                                                        ["bankAccountName"] = new OpenApiString(
                                                            "NGUYEN VAN A"
                                                        )
                                                    },
                                                    ["proofUrl"] = new OpenApiString(
                                                        "https://example.com/proof.jpg"
                                                    )
                                                }
                                            },
                                            ["totalCount"] = new OpenApiInteger(50),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["currentPage"] = new OpenApiInteger(1),
                                            ["hasNext"] = new OpenApiBoolean(true)
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Lấy lịch sử giao dịch thành công"
                                        )
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid parameters",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Invalid date range provided"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User not authorized to view these transactions"
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [AsParameters] GetTransactionHistoryRequest request
    )
    {
        var result = await sender.Send(
            new GetTransactionHistory.Query(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.TransactionType,
                request.FromDate,
                request.ToDate
            )
        );

        return result.MapResult();
    }

    public record GetTransactionHistoryRequest(
        [FromQuery(Name = "index")] int PageNumber = 1,
        [FromQuery(Name = "size")] int PageSize = 10,
        [FromQuery(Name = "keyword")] string? SearchTerm = null,
        [FromQuery(Name = "type")] string? TransactionType = null,
        [FromQuery(Name = "fromDate")] DateTimeOffset? FromDate = null,
        [FromQuery(Name = "toDate")] DateTimeOffset? ToDate = null
    );
}
