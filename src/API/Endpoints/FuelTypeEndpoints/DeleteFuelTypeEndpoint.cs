using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_FuelType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FuelTypeEndpoints;

public class DeleteFuelTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/fuel-types/{id:guid}", Handle)
            .WithSummary("Delete a fuel type")
            .WithTags("Fuel Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id
    )
    {
        Result result = await sender.Send(new DeleteFuelType.Command(id));
        return result.MapResult();
    }
}