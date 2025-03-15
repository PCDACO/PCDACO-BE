using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class ApproveLicenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/{id:guid}/license/approve", Handle)
            .WithSummary("Admin approve or reject a user license")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid id,
        ApproveLicenseRequest request
    )
    {
        Result<ApproveLicense.Response> result = await sender.Send(
            new ApproveLicense.Command(id, request.IsApproved, request.RejectReason)
        );
        return result.MapResult();
    }

    private sealed record ApproveLicenseRequest(bool IsApproved, string? RejectReason);
}
