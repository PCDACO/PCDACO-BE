using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Driver.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetLicenseByUserIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/licenses", Handle)
            .WithName("GetLicenseByUserId")
            .WithSummary("Get driver license information by user ID")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, CancellationToken cancellationToken)
    {
        Result<GetLicenseByUserId.Response> result = await sender.Send(
            new GetLicenseByUserId.Query(DriverId: id),
            cancellationToken
        );
        return result.MapResult();
    }
}
