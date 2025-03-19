using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class CreateBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings", Handle)
            .WithSummary("Create a new booking")
            .WithTags("Bookings")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new car booking.

                    Booking Rules:
                    - Only drivers with valid license can book
                    - Maximum 3 active bookings per driver
                    - Booking duration: 1-30 days
                    - Must book at least 1.5 hours in advance
                    - Car must be available and not have conflicting bookings

                    Price Calculation:
                    - Base Price = Daily Rate × Number of Days
                    - Platform Fee = 10% of Base Price
                    - Total Amount = Base Price + Platform Fee

                    Process:
                    1. Validates driver eligibility and car availability
                    2. Creates booking and contract
                    3. Sends email notifications to driver and owner
                    4. Schedules automated reminders

                    Note: This endpoint is idempotent (safe to retry)
                    """,

                    Responses =
                    {
                        ["201"] = new()
                        {
                            Description = "Created - Booking successfully created",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["id"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Tạo mới thành công")
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
                                            "Thời gian thuê phải từ 1 đến 30 ngày"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a driver or has invalid license",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bằng lái xe của bạn không hợp lệ hoặc đã hết hạn. Vui lòng cập nhật thông tin bằng lái xe trước khi đặt xe."
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car not found or not available",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe phù hợp")
                                    }
                                }
                            }
                        },
                        ["409"] = new()
                        {
                            Description = "Conflict - Car already booked for the requested period",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Xe đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác."
                                        )
                                    }
                                }
                            }
                        },
                        ["422"] = new()
                        {
                            Description = "Unprocessable Entity - Too many active bookings",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn chỉ có thể đặt tối đa 3 đơn cùng lúc"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, CreateBookingRequest request)
    {
        Result<CreateBooking.Response> result = await sender.Send(
            new CreateBooking.CreateBookingCommand(
                request.CarId,
                request.StartTime,
                request.EndTime
            )
        );

        return result.MapResult();
    }

    private sealed record CreateBookingRequest(
        Guid CarId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime
    );
}
