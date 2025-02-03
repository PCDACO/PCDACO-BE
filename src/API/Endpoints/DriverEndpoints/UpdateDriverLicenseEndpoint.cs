using API.Utils;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Driver.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.DriverEndpoints;

public class UpdateDriverLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/drivers/license", Handle)
            .WithName("UpdateDriverLicense")
            .WithSummary("Update driver license information")
            .WithTags("Drivers")
            .RequireAuthorization()
            .AddEndpointFilter<IdempotencyFilter>()
            .DisableAntiforgery()
            .Accepts<UpdateDriverLicenseRequest>("multipart/form-data");
    }

    private async Task<IResult> Handle(
        [FromForm] UpdateDriverLicenseRequest request,
        ISender sender,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateDriverLicense.Command(
            LicenseNumber: request.LicenseNumber,
            LicenseImageFrontUrl: request.LicenseImageFront.OpenReadStream(),
            LicenseImageBackUrl: request.LicenseImageBack.OpenReadStream(),
            Fullname: request.Fullname!,
            ExpirationDate: request.ExpirationDate
        );

        var result = await sender.Send(command, cancellationToken);
        return result.MapResult();
    }
}

public record UpdateDriverLicenseRequest(
    string LicenseNumber,
    IFormFile LicenseImageFront,
    IFormFile LicenseImageBack,
    DateTime ExpirationDate,
    string? Fullname = ""
);
