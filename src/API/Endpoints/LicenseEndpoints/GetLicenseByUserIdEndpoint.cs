using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetLicenseByUserIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}/license", Handle)
            .WithSummary("Get license details by user id")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetLicenseByUserId.Response> result = await sender.Send(
            new GetLicenseByUserId.Query(id)
        );
        return result.MapResult();
    }
}
