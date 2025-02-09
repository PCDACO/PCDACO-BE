using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Model.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class GetModelByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models/{id:guid}", Handle)
            .WithSummary("Get model by ID")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetModelById.Response> result = await sender.Send(new GetModelById.Query(id));
        return result.MapResult();
    }
}
