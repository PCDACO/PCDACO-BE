using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.DTOs;
using UseCases.UC_Report.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ReportEndpoints;

public class GetAllReportsByBookingIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id:guid}/reports", Handle)
            .WithSummary("Get all reports for a specific booking")
            .WithTags("Reports")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve all reports associated with a specific booking.

                    Features:
                    - Paginated list of reports
                    - Optional filtering by report status and type
                    - Reports ordered by creation date (newest first)
                    - Includes detailed information about:
                      * Report details (title, description, type, status)
                      * Reporter information
                      * Compensation details (if applicable)
                      * Resolution information (if resolved)
                      * Associated images

                    Access Control:
                    - Admins and consultants can view all reports
                    - Drivers can only view reports for their bookings
                    - Car owners can only view reports for their cars' bookings

                    Notes:
                    - All timestamps are in UTC
                    - Images are returned as URLs
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns paginated list of reports",
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
                                                    ["title"] = new OpenApiString("Xe bị hư hỏng"),
                                                    ["description"] = new OpenApiString(
                                                        "Xe bị trầy xước phần cản trước"
                                                    ),
                                                    ["reportType"] = new OpenApiString("Damage"),
                                                    ["status"] = new OpenApiString("Resolved"),
                                                    ["reporter"] = new OpenApiObject
                                                    {
                                                        ["id"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174001"
                                                        ),
                                                        ["name"] = new OpenApiString("Nguyễn Văn A")
                                                    },
                                                    ["compensation"] = new OpenApiObject
                                                    {
                                                        ["userId"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174002"
                                                        ),
                                                        ["userName"] = new OpenApiString(
                                                            "Trần Văn B"
                                                        ),
                                                        ["reason"] = new OpenApiString(
                                                            "Bồi thường trầy xước xe"
                                                        ),
                                                        ["amount"] = new OpenApiDouble(1000000),
                                                        ["isPaid"] = new OpenApiBoolean(true),
                                                        ["paidImageUrl"] = new OpenApiString(
                                                            "https://example.com/payment-proof.jpg"
                                                        ),
                                                        ["paidAt"] = new OpenApiString(
                                                            "2024-03-15T10:30:00Z"
                                                        )
                                                    },
                                                    ["resolution"] = new OpenApiObject
                                                    {
                                                        ["resolverId"] = new OpenApiString(
                                                            "123e4567-e89b-12d3-a456-426614174003"
                                                        ),
                                                        ["resolverName"] = new OpenApiString(
                                                            "Admin User"
                                                        ),
                                                        ["comments"] = new OpenApiString(
                                                            "Đã xác nhận bồi thường"
                                                        ),
                                                        ["resolvedAt"] = new OpenApiString(
                                                            "2024-03-15T11:00:00Z"
                                                        )
                                                    },
                                                    ["imageUrls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "https://example.com/damage1.jpg"
                                                        ),
                                                        new OpenApiString(
                                                            "https://example.com/damage2.jpg"
                                                        )
                                                    },
                                                    ["createdAt"] = new OpenApiString(
                                                        "2024-03-15T09:00:00Z"
                                                    )
                                                }
                                            },
                                            ["totalCount"] = new OpenApiInteger(5),
                                            ["pageNumber"] = new OpenApiInteger(1),
                                            ["pageSize"] = new OpenApiInteger(10),
                                            ["hasNext"] = new OpenApiBoolean(false)
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Lấy dữ liệu thành công")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User does not have access to this booking's reports",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền truy cập tài nguyên này"
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
                                        ["message"] = new OpenApiString("Không tìm thấy đơn đặt xe")
                                    }
                                }
                            }
                        }
                    },

                    Parameters =
                    {
                        new()
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Description = "The unique identifier of the booking",
                            Schema = new() { Type = "string", Format = "uuid" }
                        },
                        new()
                        {
                            Name = "index",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Page number (1-based indexing)",
                            Schema = new()
                            {
                                Type = "integer",
                                Default = new OpenApiInteger(1),
                                Minimum = 1
                            }
                        },
                        new()
                        {
                            Name = "size",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Number of items per page",
                            Schema = new()
                            {
                                Type = "integer",
                                Default = new OpenApiInteger(10),
                                Minimum = 1,
                                Maximum = 50
                            }
                        },
                        new()
                        {
                            Name = "status",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Filter by report status",
                            Schema = new()
                            {
                                Type = "string",
                                Enum =
                                [
                                    new OpenApiString("Pending"),
                                    new OpenApiString("Processing"),
                                    new OpenApiString("Resolved"),
                                    new OpenApiString("Rejected")
                                ]
                            }
                        },
                        new()
                        {
                            Name = "type",
                            In = ParameterLocation.Query,
                            Required = false,
                            Description = "Filter by report type",
                            Schema = new()
                            {
                                Type = "string",
                                Enum =
                                [
                                    new OpenApiString("Damage"),
                                    new OpenApiString("Accident"),
                                    new OpenApiString("Other")
                                ]
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery] BookingReportStatus? status = null,
        [FromQuery] BookingReportType? type = null
    )
    {
        Result<OffsetPaginatedResponse<GetAllReportsByBookingId.Response>> result =
            await sender.Send(
                new GetAllReportsByBookingId.Query(
                    BookingId: id,
                    PageNumber: pageNumber!.Value,
                    PageSize: pageSize!.Value,
                    Status: status,
                    Type: type
                )
            );

        return result.MapResult();
    }
}
