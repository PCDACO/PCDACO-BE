using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_License.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.LicenseEndpoints;

public class GetAllLicensesForApproveEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/licenses/approve", Handle)
            .WithSummary("Get all licenses pending approval filter by name or email of user")
            .WithTags("Licenses")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllLicensesForApprove.Response>> result =
            await sender.Send(
                new GetAllLicensesForApprove.Query(pageNumber!.Value, pageSize!.Value, keyword!)
            );
        return result.MapResult();
    }
}
