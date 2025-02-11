using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Driver.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.DriverEndpoints;

public class UploadDriverLicenseImageEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/licenses/{id:guid}/images", Handle)
            .WithName("UploadDriverLicenseImage")
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
        Result<UploadDriverLicenseImage.Response> result = await sender.Send(
            new UploadDriverLicenseImage.Command(
                LicenseId: id,
                LicenseImageFrontUrl: licenseImageFront.OpenReadStream(),
                LicenseImageBackUrl: licenseImageBack.OpenReadStream()
            ),
            cancellationToken
        );
        return result.MapResult();
    }
}
