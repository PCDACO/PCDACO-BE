using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class UpdateUserLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/licenses/{id:guid}/information", Handle)
            .WithName("UpdateUserLicense")
            .WithSummary("Update User license information")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateUserLicenseRequest request)
    {
        Result<UpdateUserLicense.Response> result = await sender.Send(
            new UpdateUserLicense.Command(
                LicenseId: id,
                LicenseNumber: request.LicenseNumber,
                ExpirationDate: request.ExpirationDate
            )
        );
        return result.MapResult();
    }

    private record UpdateUserLicenseRequest(string LicenseNumber, DateTimeOffset ExpirationDate);
}
