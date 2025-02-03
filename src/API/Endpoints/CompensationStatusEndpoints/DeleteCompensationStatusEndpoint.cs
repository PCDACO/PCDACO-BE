using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CompensationStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CompensationStatusEndpoints;

public class DeleteCompensationStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/compensation-statuses/{id:guid}", Handle)
            .WithSummary("Delete a compensation status by Id")
            .WithTags("Compensation Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteCompensationStatus.Command(id));
        return result.MapResult();
    }
}