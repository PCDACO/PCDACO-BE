using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class UploadUserLicenseImageEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/license/images", Handle)
            .WithName("UploadUserLicenseImage")
            .WithSummary("Upload user license images")
            .WithTags("Licenses")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        IFormFile licenseImageFront,
        IFormFile licenseImageBack,
        CancellationToken cancellationToken
    )
    {
        // Create copies of the streams to ensure they're fresh
        using var frontStream = new MemoryStream();
        using var backStream = new MemoryStream();

        await licenseImageFront.CopyToAsync(frontStream, cancellationToken);
        await licenseImageBack.CopyToAsync(backStream, cancellationToken);

        // Reset positions to beginning
        frontStream.Position = 0;
        backStream.Position = 0;

        Result<UploadUserLicenseImage.Response> result = await sender.Send(
            new UploadUserLicenseImage.Command(
                LicenseImageFrontUrl: frontStream,
                LicenseImageBackUrl: backStream
            ),
            cancellationToken
        );

        return result.MapResult();
    }
}
