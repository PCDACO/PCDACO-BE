using System.Security.Cryptography.Xml;
using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class StartBookingTripEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/bookings/{id:guid}/start-trip", Handle)
            .WithSummary("Driver starts a trip for a booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Allows a driver to start their booked trip when they are near the car.

                    Notes:
                    - Only the driver who made the booking can start the trip
                    - Driver must be within 10 meters of the car's GPS location
                    - Booking must be in 'ReadyForPickup' status
                    - Creates initial trip tracking record
                    - Updates booking status to 'Ongoing'
                    - Car will be marked as not returned
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Trip started",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Đã bắt đầu chuyến đi"),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid coordinates",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Vĩ độ không hợp lệ"),
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not the driver or doesn't have driver role",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện chức năng này !"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Booking or car GPS doesn't exist",
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
                            Description =
                                "Conflict - Booking is not in ReadyForPickup status or driver is too far from car",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn phải ở trong phạm vi 10m từ xe để bắt đầu chuyến đi. Hiện tại bạn cách xe 50m"
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
        StartBookingTripRequest request
    )
    {
        Result result = await sender.Send(
            new StartBookingTrip.Command(
                id,
                request.Latitude,
                request.Longitude,
                Signature: request.Signature
            )
        );
        return result.MapResult();
    }

    private sealed record StartBookingTripRequest(
        decimal Latitude,
        decimal Longitude,
        string Signature
    );
}
