using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car/{id}", Handle)
            .WithSummary("Get car by id")
            .WithTags("Cars")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetCarById.Response> result = await sender.Send(new GetCarById.Query(id));
        return result.MapResult();
    }
}