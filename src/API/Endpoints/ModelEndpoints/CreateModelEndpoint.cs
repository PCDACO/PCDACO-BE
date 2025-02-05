using API.Utils;
using Ardalis.Result;
using Carter;
using Infrastructure.Idempotency;
using MediatR;
using UseCases.UC_Model.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ModelEndpoints;

public class CreateModelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/models", Handle)
            .WithSummary("Create a model")
            .WithTags("Models")
            .AddEndpointFilter<IdempotencyFilter>()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CreateModelRequest request)
    {
        Result<CreateModel.Response> result = await sender.Send(
            new CreateModel.Command(request.Name, request.ReleaseDate, request.ManufacturerId)
        );
        return result.MapResult();
    }

    private record CreateModelRequest(string Name, DateTimeOffset ReleaseDate, Guid ManufacturerId);
}
