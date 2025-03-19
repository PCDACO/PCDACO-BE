using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Booking.Queries;

namespace API.Endpoints.BookingEndpoints;

public class GetAllBookingsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings", Handle)
            .WithSummary("Get all bookings")
            .WithTags("Bookings")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve paginated list of bookings with cursor-based pagination.

                    Access Control:
                    - Drivers: Can only view their own bookings
                    - Owners: Can only view bookings for their cars

                    Filtering Options:
                    - Search: Filter by car name, driver name, or owner name
                    - Status: Filter by booking status
                    - Payment: Filter by payment status

                    Pagination:
                    - Cursor-based pagination using lastId
                    - Default limit: 10 items per page
                    - Returns hasMore flag for additional pages
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
                                                    ["carName"] = new OpenApiString("Toyota Camry"),
                                                    ["driverName"] = new OpenApiString("John Doe"),
                                                    ["ownerName"] = new OpenApiString("Jane Smith"),
                                                    ["totalAmount"] = new OpenApiDouble(2000000),
                                                    ["totalDistance"] = new OpenApiDouble(150.5),
                                                    ["isPaid"] = new OpenApiBoolean(true),
                                                    ["status"] = new OpenApiString("Completed"),
                                                    ["startTime"] = new OpenApiString(
                                                        "2024-03-15T10:00:00Z"
                                                    ),
                                                    ["endTime"] = new OpenApiString(
                                                        "2024-03-20T10:00:00Z"
                                                    ),
                                                    ["actualReturnTime"] = new OpenApiString(
                                                        "2024-03-20T09:30:00Z"
                                                    )
                                                }
                                            },
                                            ["totalCount"] = new OpenApiInteger(50),
                                            ["limit"] = new OpenApiInteger(10),
                                            ["lastId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["hasMore"] = new OpenApiBoolean(true)
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
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
                                            "Invalid booking status provided"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to view these bookings"
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "lastId")] Guid? lastId,
        [FromQuery(Name = "search")] string? searchTerm,
        [FromQuery(Name = "status")] string? status,
        [FromQuery(Name = "isPaid")] bool? isPaid,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new GetAllBookings.Query(limit ?? 10, lastId, searchTerm, status, isPaid),
            cancellationToken
        );

        return result.MapResult();
    }
}
