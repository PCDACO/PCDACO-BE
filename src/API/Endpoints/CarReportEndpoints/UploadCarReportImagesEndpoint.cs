using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_CarReport.Commands;

namespace API.Endpoints.CarReportEndpoints;

public class UploadCarReportImagesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/car-reports/{reportId}/images", Handle)
            .WithSummary("Upload images for a car report")
            .WithTags("Car Reports")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload and update images for a specific car report.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can upload images for their car reports

                    Upload Process:
                    - Replaces all existing report images
                    - Uploads images to Cloudinary
                    - Stores image metadata in database

                    Image Requirements:
                    - Maximum size: 10MB per image
                    - Allowed formats: jpg, jpeg, png
                    - Multiple images can be uploaded simultaneously
                    - Unique naming: CarReport-{reportId}-Image-{uuid}

                    Note: This is a complete replacement operation.
                    All existing report images will be replaced with the new ones.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Images uploaded",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["images"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://cloudinary.com/car-report-123-image-1.jpg"
                                                    )
                                                },
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://cloudinary.com/car-report-123-image-2.jpg"
                                                    )
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật ảnh thành công")
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
                                            "Ảnh không được vượt quá 10MB và chỉ chấp nhận định dạng: jpg, jpeg, png"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not authorized to modify this report",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền cập nhật ảnh cho báo cáo này"
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

    private async Task<IResult> Handle(
        ISender sender,
        Guid reportId,
        IFormFileCollection files,
        CancellationToken cancellationToken
    )
    {
        UploadCarReportImages.ImageFile[] images =
        [
            .. files.Select(file => new UploadCarReportImages.ImageFile
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName
            })
        ];

        var result = await sender.Send(
            new UploadCarReportImages.Command(reportId, images),
            cancellationToken
        );

        return result.MapResult();
    }
}
