using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class PostInspectionImagesEndpoint : ICarterModule
{
    private const int MaxFileSizeInMb = 10;
    private const int MaxImagesPerType = 5;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    private const string FUEL_GAUGE_PHOTOS = "fuelGaugeFinalPhotos";
    private const string SCRATCHES_PHOTOS = "scratchesPhotos";
    private const string CLEANLINESS_PHOTOS = "cleanlinessPhotos";
    private const string TOLL_FEES_PHOTOS = "tollFeesPhotos";

    private const string FUEL_GAUGE_NOTE = "fuelGaugeFinalNote";
    private const string SCRATCHES_NOTE = "scratchesNote";
    private const string CLEANLINESS_NOTE = "cleanlinessNote";
    private const string TOLL_FEES_NOTE = "tollFeesNote";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{bookingId}/post-inspection", Handle)
            .WithSummary("Submit post-booking inspection images")
            .WithTags("Bookings")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Submit post-booking inspection images and notes after rental completion.

                    Access Control:
                    - Requires authentication
                    - Only car owners can submit inspection images
                    - Only available for completed bookings

                    Required Photo Categories:
                    1. Final Fuel Gauge Photos (fuelGaugeFinalPhotos)
                       - Shows final fuel level after rental
                    2. Cleanliness Photos (cleanlinessPhotos)
                       - Documents car cleanliness condition

                    Optional Photo Categories:
                    1. Scratches Photos (scratchesPhotos)
                       - Any damage or scratches found
                    2. Toll Fees Photos (tollFeesPhotos)
                       - Evidence of unpaid toll fees

                    File Requirements:
                    - Maximum 5 images per category
                    - Maximum file size: 10MB per image
                    - Allowed formats: .jpg, .jpeg, .png

                    Process Effects:
                    - Marks inspection as complete
                    - Automatically confirms car return
                    - Can only be submitted once
                    - Updates booking timestamps
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
                                            ["inspectionId"] = new OpenApiString(
                                                "0195ae47-6b29-74a1-abed-3134fee8d179"
                                            ),
                                            ["photos"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["type"] = new OpenApiInteger(5),
                                                    ["urls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "http://example.com/fuelGaugeFinal.jpg"
                                                        )
                                                    }
                                                },
                                                new OpenApiObject
                                                {
                                                    ["type"] = new OpenApiInteger(7),
                                                    ["urls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "http://example.com/cleanliness.jpg"
                                                        )
                                                    }
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("")
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
                                            "Thiếu hình ảnh bắt buộc: hình ảnh mức xăng cuối, hình ảnh vệ sinh xe"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not the car owner or booking is not completed",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền phê duyệt booking cho xe này!"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Booking doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy đặt xe")
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
        Guid bookingId,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var photos = new List<PostInspectionImages.PhotoRequest>();

        // Validate all required photo types are present
        ValidateRequiredPhotoTypes(form);

        ProcessPhotos(form, photos);

        var command = new PostInspectionImages.Command(bookingId, photos);

        var result = await sender.Send(command, cancellationToken);

        return result.MapResult();
    }

    private static void ProcessPhotos(
        IFormCollection form,
        List<PostInspectionImages.PhotoRequest> photos
    )
    {
        // Process required photos
        AddPhotoRequest(
            form,
            FUEL_GAUGE_PHOTOS,
            FUEL_GAUGE_NOTE,
            InspectionPhotoType.FuelGaugeFinal,
            photos
        );
        AddPhotoRequest(
            form,
            CLEANLINESS_PHOTOS,
            CLEANLINESS_NOTE,
            InspectionPhotoType.Cleanliness,
            photos
        );

        // Process optional photos if provided
        if (form.Files.GetFiles(SCRATCHES_PHOTOS).Any())
        {
            AddPhotoRequest(
                form,
                SCRATCHES_PHOTOS,
                SCRATCHES_NOTE,
                InspectionPhotoType.Scratches,
                photos
            );
        }

        if (form.Files.GetFiles(TOLL_FEES_PHOTOS).Any())
        {
            AddPhotoRequest(
                form,
                TOLL_FEES_PHOTOS,
                TOLL_FEES_NOTE,
                InspectionPhotoType.TollFees,
                photos
            );
        }
    }

    private static void AddPhotoRequest(
        IFormCollection form,
        string photoField,
        string noteField,
        InspectionPhotoType photoType,
        List<PostInspectionImages.PhotoRequest> photos
    )
    {
        var files = form.Files.GetFiles(photoField);
        ValidateFiles(photoField, files);

        photos.Add(
            new PostInspectionImages.PhotoRequest(photoType, [.. files], form[noteField].ToString())
        );
    }

    private static void ValidateRequiredPhotoTypes(IFormCollection form)
    {
        var requiredPhotoTypes = new[]
        {
            (FUEL_GAUGE_PHOTOS, "hình ảnh mức xăng cuối"),
            (CLEANLINESS_PHOTOS, "hình ảnh vệ sinh xe")
        };

        var missingTypes = requiredPhotoTypes
            .Where(type => !form.Files.GetFiles(type.Item1).Any())
            .Select(type => type.Item2)
            .ToList();

        if (missingTypes.Any())
        {
            throw new InvalidOperationException(
                $"Thiếu hình ảnh bắt buộc: {string.Join(", ", missingTypes)}"
            );
        }
    }

    private static void ValidateFiles(string category, IEnumerable<IFormFile> files)
    {
        if (!files.Any())
        {
            throw new InvalidOperationException(
                $"Yêu cầu ít nhất một hình ảnh cho {GetCategoryDisplayName(category)}"
            );
        }

        if (files.Count() > MaxImagesPerType)
        {
            throw new InvalidOperationException(
                $"Không được tải lên quá {MaxImagesPerType} ảnh cho {GetCategoryDisplayName(category)}"
            );
        }

        foreach (var file in files)
        {
            if (!ValidateFile(file, out string error))
            {
                throw new InvalidOperationException(error);
            }
        }
    }

    private static string GetCategoryDisplayName(string category) =>
        category switch
        {
            FUEL_GAUGE_PHOTOS => "hình ảnh mức xăng cuối",
            SCRATCHES_PHOTOS => "hình ảnh vết xước",
            CLEANLINESS_PHOTOS => "hình ảnh vệ sinh xe",
            TOLL_FEES_PHOTOS => "hình ảnh phí cầu đường",
            _ => category
        };

    private static bool ValidateFile(IFormFile file, out string error)
    {
        error = string.Empty;

        if (file.Length > MaxFileSizeInMb * 1024 * 1024)
        {
            error = $"File {file.FileName} vượt quá {MaxFileSizeInMb}MB";
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error =
                $"File {file.FileName} không đúng định dạng. Chỉ chấp nhận {string.Join(", ", AllowedExtensions)}";
            return false;
        }

        return true;
    }
}
