using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class PreInspectionImagesEndpoint : ICarterModule
{
    private const int MaxFileSizeInMb = 10;
    private const int MaxImagesPerType = 5;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public const string EXTERIOR_PHOTOS = "exteriorPhotos";
    public const string FUEL_GAUGE_PHOTOS = "fuelGaugePhotos";
    public const string PARKING_LOCATION_PHOTOS = "parkingLocationPhotos";
    public const string CAR_KEY_PHOTOS = "carKeyPhotos";
    public const string TRUNK_PHOTOS = "trunkPhotos";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings/{bookingId}/pre-inspection", Handle)
            .WithSummary("Submit pre-booking inspection images")
            .WithTags("Bookings")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Submit pre-booking inspection images and notes for a car before rental pickup.

                    Access Control:
                    - Requires authentication
                    - Only car owners can submit inspection images
                    - Can only be submitted within 24 hours before booking start time

                    Required Photo Categories:
                    1. Exterior Car Photos (exteriorPhotos)
                    2. Fuel Gauge Photos (fuelGaugePhotos)
                    3. Car Key Photos (carKeyPhotos)
                    4. Trunk Space Photos (trunkPhotos)

                    Optional Photo Categories:
                    - Parking Location Photos (parkingLocationPhotos)

                    File Requirements:
                    - Maximum 5 images per category
                    - Maximum file size: 10MB per image
                    - Allowed formats: .jpg, .jpeg, .png

                    Notes:
                    - Each category can include an optional text note
                    - Inspection is marked complete only when all required photos are uploaded
                    - Booking status updates to ReadyForPickup when inspection is complete
                    - Can only be updated once per booking
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
                                            ["inspectionId"] = new OpenApiString("guid"),
                                            ["photos"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["type"] = new OpenApiInteger(1),
                                                    ["urls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "http://example.com/photo1.jpg"
                                                        )
                                                    }
                                                },
                                                new OpenApiObject
                                                {
                                                    ["type"] = new OpenApiInteger(2),
                                                    ["urls"] = new OpenApiArray
                                                    {
                                                        new OpenApiString(
                                                            "http://example.com/photo2.jpg"
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
                                            "Thiếu hình ảnh bắt buộc: hình ảnh ngoại thất xe"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description =
                                "Forbidden - User is not the car owner or booking is not in valid state",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện chức năng này với booking này!"
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
        var photos = new List<PreInspectionImages.PhotoRequest>();

        ValidateRequiredPhotoTypes(form);

        ProcessPhotos(form, photos);

        var command = new PreInspectionImages.Command { BookingId = bookingId, Photos = photos };

        var result = await sender.Send(command, cancellationToken);

        return result.MapResult();
    }

    private static void ProcessPhotos(
        IFormCollection form,
        List<PreInspectionImages.PhotoRequest> photos
    )
    {
        // Process exterior car photos
        ValidateFiles(EXTERIOR_PHOTOS, form.Files.GetFiles(EXTERIOR_PHOTOS));
        photos.Add(
            new PreInspectionImages.PhotoRequest(
                InspectionPhotoType.ExteriorCar,
                [.. form.Files.GetFiles(EXTERIOR_PHOTOS)],
                form["exteriorNote"].ToString()
            )
        );

        // Process fuel gauge photos
        ValidateFiles(FUEL_GAUGE_PHOTOS, form.Files.GetFiles(FUEL_GAUGE_PHOTOS));
        photos.Add(
            new PreInspectionImages.PhotoRequest(
                InspectionPhotoType.FuelGauge,
                [.. form.Files.GetFiles(FUEL_GAUGE_PHOTOS)],
                form["fuelGaugeNote"].ToString()
            )
        );

        ValidateFiles(PARKING_LOCATION_PHOTOS, form.Files.GetFiles(PARKING_LOCATION_PHOTOS));
        photos.Add(
            new PreInspectionImages.PhotoRequest(
                InspectionPhotoType.ParkingLocation,
                [.. form.Files.GetFiles(PARKING_LOCATION_PHOTOS)],
                form["parkingLocationNote"].ToString()
            )
        );

        // Process car key photos
        ValidateFiles(CAR_KEY_PHOTOS, form.Files.GetFiles(CAR_KEY_PHOTOS));
        photos.Add(
            new PreInspectionImages.PhotoRequest(
                InspectionPhotoType.CarKey,
                [.. form.Files.GetFiles(CAR_KEY_PHOTOS)],
                form["carKeyNote"].ToString()
            )
        );

        // Process trunk photos
        ValidateFiles(TRUNK_PHOTOS, form.Files.GetFiles(TRUNK_PHOTOS));
        photos.Add(
            new PreInspectionImages.PhotoRequest(
                InspectionPhotoType.TrunkSpace,
                [.. form.Files.GetFiles(TRUNK_PHOTOS)],
                form["trunkNote"].ToString()
            )
        );
    }

    private static void ValidateRequiredPhotoTypes(IFormCollection form)
    {
        var requiredPhotoTypes = new[]
        {
            EXTERIOR_PHOTOS,
            FUEL_GAUGE_PHOTOS,
            CAR_KEY_PHOTOS,
            TRUNK_PHOTOS
        };

        var missingTypes = requiredPhotoTypes
            .Where(type => !form.Files.GetFiles(type).Any())
            .ToList();

        if (missingTypes.Any())
        {
            var missingTypeNames = missingTypes.Select(type =>
                type switch
                {
                    EXTERIOR_PHOTOS => "hình ảnh ngoại thất xe",
                    FUEL_GAUGE_PHOTOS => "hình ảnh mức xăng",
                    PARKING_LOCATION_PHOTOS => "hình ảnh vị trí đỗ xe",
                    CAR_KEY_PHOTOS => "hình ảnh chìa khóa xe",
                    TRUNK_PHOTOS => "hình ảnh cốp xe",
                    _ => type
                }
            );

            throw new InvalidOperationException(
                $"Thiếu hình ảnh bắt buộc: {string.Join(", ", missingTypeNames)}"
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
            EXTERIOR_PHOTOS => "hình ảnh ngoại thất xe",
            FUEL_GAUGE_PHOTOS => "hình ảnh mức xăng",
            CAR_KEY_PHOTOS => "hình ảnh chìa khóa xe",
            TRUNK_PHOTOS => "hình ảnh cốp xe",
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
