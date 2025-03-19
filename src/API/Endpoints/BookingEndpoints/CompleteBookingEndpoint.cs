using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CompleteBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/complete", Handle)
            .WithSummary("Driver complete a booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allow driver to complete their booking and calculate final payment.

                    Return Location Rules:
                    - Must return car within 100 meters of the original pickup location
                    - Current GPS coordinates required for validation

                    Early Return Policy:
                    - If returned before half of the booking duration
                    - 50% refund for unused days

                    Late Return Policy:
                    - Additional fee of 120% of daily rate for excess days

                    Final Amount Calculation:
                    - Base Price: Original booking amount
                    - Platform Fee: Service fee
                    - Excess Fee: Late return penalties (if any)
                    - Refund Amount: Early return refund (if applicable)
                    - Final Amount = Base Price + Platform Fee + Excess Fee - Refund Amount

                    Notes:
                    - Only the booking driver can complete their booking
                    - Booking must be in 'Ongoing' status
                    - Total distance traveled will be calculated
                    - Email notifications will be sent to both driver and owner
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
                                            ["totalDistance"] = new OpenApiDouble(150.5), // in kilometers
                                            ["unusedDays"] = new OpenApiDouble(2),
                                            ["refundAmount"] = new OpenApiDouble(500000),
                                            ["excessDays"] = new OpenApiDouble(0),
                                            ["excessFee"] = new OpenApiDouble(0),
                                            ["basePrice"] = new OpenApiDouble(2000000),
                                            ["platformFee"] = new OpenApiDouble(200000),
                                            ["finalAmount"] = new OpenApiDouble(1700000)
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid location",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn phải trả xe tại địa điểm đã đón xe: 123 ABC Street. Vui lòng di chuyển đến trong phạm vi 100 mét so với vị trí đón xe!"
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
                            Description = "Conflict - Booking is not in ongoing status",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể phê duyệt booking ở trạng thái Completed"
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
        CompleteBookingRequest request
    )
    {
        var result = await sender.Send(
            new CompleteBooking.Command(id, request.CurrentLatitude, request.CurrentLongitude)
        );
        return result.MapResult();
    }

    private sealed record CompleteBookingRequest(decimal CurrentLatitude, decimal CurrentLongitude);
}
