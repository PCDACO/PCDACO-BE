using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class ApproveBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/approve", Handle)
            .WithSummary("Owner approve or reject a booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allow car owner to approve or reject a booking request.

                    Notes:
                    - Only the car owner can approve/reject bookings for their cars
                    - When approved:
                      * If booking is not paid, payment token will be generated
                      * Overlapping pending bookings will be automatically rejected
                      * Contract will be updated with owner's signature
                      * If not paid, booking will expire after 12 hours
                    - Email notifications will be sent to the driver
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
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Đã phê duyệt booking thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid input",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Booking id không được để trống"
                                        ),
                                    },
                                },
                            },
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
                                            "Bạn không có quyền phê duyệt booking cho xe này!"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Booking doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy booking"),
                                    },
                                },
                            },
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Booking is not in pending status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể phê duyệt booking ở trạng thái Approved"
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
        ApproveBookingRequest request,
        HttpContext context
    )
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        Result result = await sender.Send(
            new ApproveBooking.Command(
                id,
                request.IsApproved,
                baseUrl,
                Signature: request.Signature
            )
        );

        return result.MapResult();
    }

    private sealed record ApproveBookingRequest(bool IsApproved, string Signature);
}
