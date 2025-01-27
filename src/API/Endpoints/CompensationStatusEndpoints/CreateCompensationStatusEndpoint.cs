using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_CompensationStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CompensationStatusEndpoints;

public class CreateCompensationStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/compensation-statuses", Handle)
            .WithSummary("Creates a new compensation status")
            .WithTags("Compensation Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CreateCompensationStatusRequest request)
    {
        Result<CreateCompensationStatus.Response> result = await sender
            .Send(new CreateCompensationStatus.Command(request.Name));
        return result.MapResult();
    }
    private record CreateCompensationStatusRequest(string Name);
}