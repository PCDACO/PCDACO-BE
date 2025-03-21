using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
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
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        InspectionPhotoType photoType,
        IFormFileCollection photos,
        string description = "",
        DateTimeOffset? expiryDate = null,
        CancellationToken cancellationToken = default
    )
    {
        // Open streams for all images
        Stream[] streams = [.. photos.Select(p => p.OpenReadStream())];

        var result = await sender.Send(
            new UploadInspectionSchedulePhotos.Command(id, photoType, streams, description, expiryDate),
            cancellationToken
        );

        return result.MapResult();
    }
}
