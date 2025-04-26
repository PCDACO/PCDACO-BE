using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class ExtendBookingDayEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/extend", Handle)
            .WithSummary("Extend an existing booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Extend an existing car booking.

                    Booking Rules:
                    - Users can extend the booking duration before the trip starts.
                    - If the booking is ongoing, users can only extend the end date.
                    - Payment is required within 15 minutes of extending an ongoing booking.

                    Process:
                    1. Validates booking status and user eligibility.
                    2. Updates the booking dates.
                    3. Sends email notifications to driver and owner if necessary.
                    4. Schedules a job to revert the booking if payment is not made.

                    Note: This endpoint is idempotent (safe to retry)
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "OK - Booking successfully extended",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["bookingId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["newStartDate"] = new OpenApiString(
                                                "2023-01-05T00:00:00Z"
                                            ),
                                            ["newEndDate"] = new OpenApiString(
                                                "2023-01-15T00:00:00Z"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Gia hạn thành công")
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
                                            "Thời gian bắt đầu không hợp lệ"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User does not have permission"
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
                        ["409"] = new()
                        {
                            Description = "Conflict - Booking cannot be extended",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không thể gia hạn booking này"
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
        ExtendBookingDayRequest request
    )
    {
        Result<ExtendBookingDay.Response> result = await sender.Send(
            new ExtendBookingDay.Command(id, request.NewStartTime, request.NewEndTime)
        );

        return result.MapResult();
    }

    private sealed record ExtendBookingDayRequest(
        DateTimeOffset NewStartTime,
        DateTimeOffset NewEndTime
    );
}
