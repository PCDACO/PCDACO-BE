using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Driver.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.DriverEndpoints;

public class AddDriverLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{id:guid}/license", Handle)
            .WithName("AddDriverLicense")
            .WithSummary("Add driver license information")
            .WithTags("Licenses")
            .RequireAuthorization()
            .AddEndpointFilter<IdempotencyFilter>()
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateDriverLicenseRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<AddDriverLicense.Response> result = await sender.Send(
            new AddDriverLicense.Command(
                DriverId: id,
                LicenseNumber: request.LicenseNumber,
                ExpirationDate: request.ExpirationDate
            ),
            cancellationToken
        );
        return result.MapResult();
    }
}

public record UpdateDriverLicenseRequest(string LicenseNumber, DateTime ExpirationDate);
