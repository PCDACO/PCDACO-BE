using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Driver.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetLicenseByCurrentDriverEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/licenses/driver", Handle)
            .WithName("GetLicenseByCurrentDriver")
            .WithSummary("Get driver license information by current driver")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        Result<GetLicenseByCurrentDriver.Response> result = await sender.Send(
            new GetLicenseByCurrentDriver.Query(),
            cancellationToken
        );
        return result.MapResult();
    }
}
