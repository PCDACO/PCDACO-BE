using API.Utils;
using Carter;
using Domain.Enums;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_CarReport.Commands;

namespace API.Endpoints.CarReportEndpoints;

public class CreateCarReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/car-reports", Handle)
            .WithSummary("Create a new car report")
            .WithTags("Car Reports")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Create a new report for a car.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can create reports for their cars

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
                                            "Báo cáo xe đã được tạo thành công"
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
                            Description = "Forbidden - User is not the car owner",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền tạo báo cáo cho xe này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe")
                                    }
                                }
                            }
                        }
                    },
                    RequestBody = new()
                    {
                        Description = "Car report details",
                        Required = true,
                        Content =
                        {
                            ["application/json"] = new()
                            {
                                Example = new OpenApiObject
                                {
                                    ["carId"] = new OpenApiString(
                                        "123e4567-e89b-12d3-a456-426614174000"
                                    ),
                                    ["title"] = new OpenApiString("Hư hỏng động cơ"),
                                    ["description"] = new OpenApiString(
                                        "Động cơ phát ra tiếng ồn lạ khi khởi động"
                                    ),
                                    ["reportType"] = new OpenApiString("MechanicalIssue")
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        CreateCarReportRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new CreateCarReport.Command(
                request.CarId,
                request.Title,
                request.Description,
                request.ReportType
            ),
            cancellationToken
        );

        return result.MapResult();
    }

    private sealed record CreateCarReportRequest(
        Guid CarId,
        string Title,
        string Description,
        CarReportType ReportType
    );
}
