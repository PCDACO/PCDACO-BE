using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_License.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetLicenseByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/licenses/{id:guid}", Handle)
            .WithSummary("Get license details by id")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetLicenseById.Response> result = await sender.Send(new GetLicenseById.Query(id));
        return result.MapResult();
    }
}
