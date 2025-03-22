using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Manufacturer.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class UploadManufacturerLogoEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/manufacturers/{id:guid}/logo", Handle)
            .WithSummary("Upload manufacturer logo image")
            .WithTags("Manufacturers")
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        IFormFile logo,
        CancellationToken cancellationToken
    )
    {
        Result<UploadManufacturerLogo.Response> result = await sender.Send(
            new UploadManufacturerLogo.Command(id, logo.OpenReadStream()),
            cancellationToken
        );
        return result.MapResult();
    }
}
