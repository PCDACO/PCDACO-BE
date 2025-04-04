using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Report.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ReportEndpoints;

public class ProvideCompensationPaidImageEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/reports/{reportId}/compensation-proof", Handle)
            .WithSummary("Provide compensation payment proof")
            .WithTags("Reports")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload compensation payment proof image.

                    Process:
                    1. Validates user permissions (driver or owner)
                    2. Uploads image to cloud storage
                    3. Updates report with image URL
                    4. Triggers review process

                    Note: Only drivers and owners can provide payment proof
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Payment proof uploaded",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["reportId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["imageUrl"] = new OpenApiString(
                                                "https://example.com/proof.jpg"
                                            )
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Ảnh đã được cung cấp thành công"
                                        )
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid image format or size",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Ảnh không hợp lệ")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not authorized",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện thao tác này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Report not found",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy báo cáo")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(ISender sender, Guid reportId, IFormFile images)
    {
        using var stream = images.OpenReadStream();
        Result<ProvideCompensationPaidImage.Response> result = await sender.Send(
            new ProvideCompensationPaidImage.Command(reportId, stream)
        );

        return result.MapResult();
    }
}
