using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Model.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class UpdateModelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/models/{id}", Handle)
            .WithSummary("Update a model")
            .WithTags("Models")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateModelRequest request)
    {
        Result<UpdateModel.Response> result = await sender.Send(
            new UpdateModel.Command(id, request.Name, request.ReleaseDate, request.ManufacturerId)
        );
        return result.MapResult();
    }

    private record UpdateModelRequest(string Name, DateTime ReleaseDate, Guid ManufacturerId);
}
