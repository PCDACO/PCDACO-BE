using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_FuelType.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.FuelTypeEndpoints;

public class CreateFuelTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/fuel-types", Handle)
            .WithSummary("Create a new fuel type")
            .WithTags("Fuel Types")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        CreateFuelTypeRequest request)
    {
        Result<CreateFuelType.Response> result = await sender.Send(new CreateFuelType.Command(request.Name));
        return result.MapResult();
    }
    private record CreateFuelTypeRequest(string Name);
}