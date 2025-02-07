using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Model.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class DeleteModelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/models/{id:guid}", Handle)
            .WithSummary("Delete a model")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteModel.Command(id));
        return result.MapResult();
    }
}
