using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Manufacturer;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class CreateManufacturerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/manufacturers", Handle)
            .WithSummary("Create a new manufacturer")
            .WithTags("Manufacturers")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CreateManufacturerRequest request)
    {
        Result<CreateManufacturer.Response> result = await sender.Send(
            new CreateManufacturer.Command(request.Name)
        );
        return result.MapResult();
    }

    private record CreateManufacturerRequest(string Name);
}
