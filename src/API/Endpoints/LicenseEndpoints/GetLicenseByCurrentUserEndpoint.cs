using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetLicenseByCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/licenses/users/current", Handle)
            .WithName("GetLicenseByCurrentUser")
            .WithSummary("Get user license information by current user for owner and driver")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        Result<GetLicenseByCurrentUser.Response> result = await sender.Send(
            new GetLicenseByCurrentUser.Query(),
            cancellationToken
        );
        return result.MapResult();
    }
}
