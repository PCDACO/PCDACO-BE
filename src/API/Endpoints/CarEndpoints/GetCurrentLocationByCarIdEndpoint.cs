using API.Utils;
using Carter;
using MediatR;
using UseCases.UC_Car.Queries;

namespace API.Endpoints.CarEndpoints;

public class GetCurrentLocationByCarIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/{id:guid}/current-location", Handle)
            .WithSummary("Get Current Location By Car Id")
            .WithTags("Cars")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCurrentLocationByCarId.Query(id));
        return result.MapResult();
    }
}
