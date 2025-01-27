using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CompensationStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CompensationStatusEndpoints;

public class UpdateCompensationStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/compensation-statuses/{id:guid}", Handle)
            .WithSummary("Update a compensation status")
            .WithTags("Compensation Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, UpdateCompensationStatusRequest request)
    {
        Result result = await sender.Send(new UpdateCompensationStatus.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateCompensationStatusRequest(string Name);
}