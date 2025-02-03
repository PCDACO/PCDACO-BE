using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_FuelType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FuelTypeEndpoints;

public class UpdateFuelTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/fuel-types/{id:guid}", Handle)
            .WithSummary("Update a fuel type")
            .WithTags("Fuel Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateFuelTypeRequest request
    )
    {
        Result result = await sender.Send(new UpdateFuelType.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateFuelTypeRequest(string Name);
}