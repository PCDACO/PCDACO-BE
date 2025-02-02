using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_FuelType.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FuelTypeEndpoints;

public class GetFuelTypeByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/fuel-types/{id:guid}", Handle)
            .WithSummary("Get a fuel type by id")
            .WithTags("Fuel Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetFuelTypeById.Response> result = await sender.Send(new GetFuelTypeById.Query(id));
        return result.MapResult();
    }
}