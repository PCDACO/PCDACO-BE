using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using UseCases.UC_InspectionSchedule.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.InspectionScheduleEndpoints;

public class UploadInspectionPhotosEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/inspection-schedules/{id:guid}/photos", Handle)
            .WithName("UploadInspectionSchedulePhotos")
            .WithSummary("Upload inspection schedule photos for a specific inspection schedule")
            .WithTags("Inspection Schedules")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload photos for an inspection schedule.

                    Features:
                    - Upload one or more photos for inspection schedules
                    - Support for various photo types (exterior, interior, VIN, etc.)
                    - Add optional descriptions for photos
                    - Add expiry date for vehicle inspection certificate photos

                    Notes:
                    - Only technicians can upload inspection photos
                    - Only active inspection schedules (InProgress/Signed) support photo uploads
                    - Maximum file size: 10MB per photo
                    - Supported formats: jpg, jpeg, png, gif, bmp, tiff, webp, svg, heic, heif
                    - When uploading VehicleInspectionCertificate photos, expiry date is required
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Photos uploaded successfully",
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
                                                    ["inspectionScheduleId"] = new OpenApiString(
                                                        "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://res.cloudinary.com/example/image/upload/v1234567890/Inspection-abc123-Exterior-def456.jpg"
                                                    ),
                                                },
                                                new OpenApiObject
                                                {
                                                    ["inspectionScheduleId"] = new OpenApiString(
                                                        "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://res.cloudinary.com/example/image/upload/v1234567890/Inspection-abc123-Exterior-ghi789.jpg"
                                                    ),
                                                },
                                            },
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Tải lên ảnh kiểm định thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description =
                                "Bad Request - Invalid schedule status or validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Chỉ có thể tải lên ảnh kiểm định sau khi đã bắt đầu kiểm định và trước khi bị quá hạn kiểm định"
                                        ),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Yêu cầu ít nhất một ảnh kiểm định"),
                                            new OpenApiString(
                                                "Kích thước ảnh không được vượt quá 10MB"
                                            ),
                                            new OpenApiString(
                                                "Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif, .bmp, .tiff, .webp, .svg, .heic, .heif"
                                            ),
                                            new OpenApiString(
                                                "Ngày hết hạn giấy kiểm định không được để trống"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not a technician",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện chức năng này"
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Inspection schedule doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không tìm thấy thông tin kiểm định xe"
                                        ),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromForm] InspectionPhotoType photoType,
        [FromForm] IFormFileCollection photos,
        [FromForm] string description = "",
        [FromForm] DateTimeOffset? expiryDate = null,
        CancellationToken cancellationToken = default
    )
    {
        // Open streams for all images
        Stream[] streams = [.. photos.Select(p => p.OpenReadStream())];

        var result = await sender.Send(
            new UploadInspectionSchedulePhotos.Command(
                id,
                photoType,
                streams,
                description,
                expiryDate
            ),
            cancellationToken
        );

        return result.MapResult();
    }
}
