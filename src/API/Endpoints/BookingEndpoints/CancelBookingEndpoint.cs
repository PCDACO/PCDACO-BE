using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CancelBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/cancel", Handle)
            .WithSummary("Driver cancel a booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allow driver to cancel their booking.

                    Refund Policy:
                    - Cancel 7+ days before start: 100% refund
                    - Cancel 5-7 days before start: 50% refund
                    - Cancel 3-5 days before start: 30% refund
                    - Cancel less than 3 days before start: No refund

                    Restrictions:
                    - Only the booking driver can cancel their own booking
                    - Maximum 5 cancellations allowed per 30 days
                    - Cannot cancel bookings that are:
                      * Rejected
                      * Ongoing
                      * Completed
                      * Already Cancelled
                      * Expired
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
                                            "Đã hủy booking thành công. Số tiền hoàn trả: 500,000 VND (50%)"
                                        )
                                    }
                                }
                            }
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
                                            "Bạn đã hủy quá số lần cho phép trong 30 ngày"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not the booking driver",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện chức năng này với booking này!"
                                        )
                                    }
                                }
                            }
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
                                        ["message"] = new OpenApiString("Không tìm thấy booking")
                                    }
                                }
                            }
                        },
                        ["409"] = new()
                        {
                            Description =
                                "Conflict - Booking cannot be cancelled in current status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể hủy booking ở trạng thái Completed"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid id, string cancelReason = "")
    {
        Result result = await sender.Send(new CancelBooking.Command(id, cancelReason));
        return result.MapResult();
    }
}
