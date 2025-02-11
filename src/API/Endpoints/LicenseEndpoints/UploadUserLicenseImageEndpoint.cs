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
        app.MapPatch("/api/licenses/{id:guid}/images", Handle)
            .WithName("UploadUserLicenseImage")
            .WithSummary("Upload user license images")
            .WithTags("Licenses")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        IFormFile licenseImageFront,
        IFormFile licenseImageBack,
        CancellationToken cancellationToken
    )
    {
        Result<UploadUserLicenseImage.Response> result = await sender.Send(
            new UploadUserLicenseImage.Command(
                LicenseId: id,
                LicenseImageFrontUrl: licenseImageFront.OpenReadStream(),
                LicenseImageBackUrl: licenseImageBack.OpenReadStream()
            ),
            cancellationToken
        );
        return result.MapResult();
    }
}