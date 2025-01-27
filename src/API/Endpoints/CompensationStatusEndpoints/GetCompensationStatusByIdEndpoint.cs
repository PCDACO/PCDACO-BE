using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CompensationStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CompensationStatusEndpoints;

public class GetCompensationStatusByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/compensation-status/{id:guid}", Handle)
            .WithSummary("Get a compensation status by id")
            .WithTags("Compensation Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetCompensationStatusById.Response> result = await sender.Send(new GetCompensationStatusById.Query(id));
        return result.MapResult();
    }
}