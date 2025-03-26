using API.Utils;
using Carter;
using Domain.Enums;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Report.Commands;

namespace API.Endpoints.ReportEndpoints;

public class CreateReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports", Handle)
            .WithSummary("Create a new report")
            .WithTags("Reports")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new report for a booking.

                    Access Control:
                    - Requires authentication
                    - Only the driver or car owner involved in the booking can create reports

                    Process:
                    1. Validates user permissions
                    2. Creates report with basic details
                    3. Sets initial status to Pending

                    Note: Images should be uploaded separately using the upload endpoint
                    """,

                    Responses =
                    {
                        ["201"] = new()
                        {
                            Description = "Created - Report successfully created",
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
                                        ["message"] = new OpenApiString(
                                            "Báo cáo đã được tạo thành công"
                                        )
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
                                            "Validation errors:\n"
                                                + "- Tiêu đề không được để trống\n"
                                                + "- Mô tả không được quá 1000 ký tự"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not involved in the booking",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền tạo báo cáo cho đơn đặt xe này"
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
                    RequestBody = new()
                    {
                        Description = "Report details",
                        Required = true,
                        Content =
                        {
                            ["application/json"] = new()
                            {
                                Example = new OpenApiObject
                                {
                                    ["bookingId"] = new OpenApiString(
                                        "123e4567-e89b-12d3-a456-426614174000"
                                    ),
                                    ["title"] = new OpenApiString("Damage to Front Bumper"),
                                    ["description"] = new OpenApiString(
                                        "Found scratches and dents on the front bumper after return"
                                    ),
                                    ["reportType"] = new OpenApiString("CarDamage")
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        CreateReportRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateReport.Command(
                request.BookingId,
                request.Title,
                request.Description,
                request.ReportType
            ),
            cancellationToken
        );

        return result.MapResult();
    }

    private sealed record CreateReportRequest(
        Guid BookingId,
        string Title,
        string Description,
        BookingReportType ReportType
    );
}
