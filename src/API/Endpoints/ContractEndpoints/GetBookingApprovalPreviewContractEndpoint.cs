using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetBookingApprovalPreviewContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id:guid}/approval-preview-contract", Handle)
            .WithSummary("Get booking contract preview for owner approval")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Generate a preview of the booking contract for car owners before approving a booking request.

                    This endpoint allows owners to:
                    - View the complete contract before approving a booking
                    - Verify all rental details including driver information
                    - Review terms and conditions
                    - Make an informed decision before approving

                    Notes:
                    - Only accessible by the car owner
                    - Only works for bookings in Pending status
                    - Shows actual booking details as submitted by the driver
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns HTML content of the contract preview",
                            Content =
                            {
                                ["text/html"] = new() { Schema = new() { Type = "string" } }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not the car owner",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền xem hợp đồng này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Booking not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy booking")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Booking not in pending status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Booking không ở trạng thái chờ phê duyệt"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetBookingApprovalPreviewContract.Query(id));

        if (!result.IsSuccess)
            return Results.NotFound(result.Errors);

        return Results.Content(result.Value.HtmlContent, "text/html");
    }
}
