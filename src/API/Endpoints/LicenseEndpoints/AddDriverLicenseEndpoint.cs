using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using UseCases.UC_Driver.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class AddDriverLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{id:guid}/licenses", Handle)
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
        AddDriverLicenseRequest request,
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

public record AddDriverLicenseRequest(string LicenseNumber, DateTime ExpirationDate);
