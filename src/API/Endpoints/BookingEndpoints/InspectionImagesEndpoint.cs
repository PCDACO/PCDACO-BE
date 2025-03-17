using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using UseCases.UC_Booking.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.BookingEndpoints;

public class InspectionImagesEndpoint : ICarterModule
{
    private const int MaxFileSizeInMb = 10;
    private const int MaxImagesPerRequest = 5;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/bookings/{bookingId}/inspection", Handle)
            .WithSummary("Submit inspection images for a booking")
            .WithTags("Bookings")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid bookingId,
        InspectionType inspectionType,
        InspectionPhotoType inspectionPhotoType,
        IFormFileCollection images,
        string note = ""
    )
    {
        // Validate number of images
        if (images.Count > MaxImagesPerRequest)
        {
            return Results.BadRequest($"Không được tải lên quá {MaxImagesPerRequest} ảnh mỗi lần");
        }

        // Validate each file
        foreach (var image in images)
        {
            if (!ValidateFile(image, out string error))
            {
                return Results.BadRequest(error);
            }
        }

        // Open streams for all valid images
        var streams = images.Select(i => i.OpenReadStream()).ToArray();

        var result = await sender.Send(
            new InspectionImages.Command(
                bookingId,
                inspectionType,
                inspectionPhotoType,
                streams,
                note
            )
        );

        return result.MapResult();
    }

    private static bool ValidateFile(IFormFile file, out string error)
    {
        error = string.Empty;

        // Check file size
        if (file.Length > MaxFileSizeInMb * 1024 * 1024)
        {
            error = $"File {file.FileName} vượt quá {MaxFileSizeInMb}MB";
            return false;
        }

        // Check file extension
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
