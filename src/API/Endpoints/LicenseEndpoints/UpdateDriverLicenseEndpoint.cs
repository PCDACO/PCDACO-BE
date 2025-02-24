using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Driver.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class UpdateDriverLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/licenses/{id:guid}/information", Handle)
            .WithName("UpdateDriverLicense")
            .WithSummary("Update driver license information")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateDriverLicenseRequest request)
    {
        Result<UpdateDriverLicense.Response> result = await sender.Send(
            new UpdateDriverLicense.Command(
                LicenseId: id,
                LicenseNumber: request.LicenseNumber,
                ExpirationDate: request.ExpirationDate
            )
        );
        return result.MapResult();
    }

    private record UpdateDriverLicenseRequest(string LicenseNumber, DateTimeOffset ExpirationDate);
}
