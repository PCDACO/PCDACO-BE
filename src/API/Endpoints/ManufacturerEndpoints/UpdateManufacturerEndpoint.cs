using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Manufacturer;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class UpdateManufacturerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/manufacturers/{id:guid}", Handle)
            .WithSummary("Update a manufacturer")
            .WithTags("Manufacturers")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateManufacturerRequest request)
    {
        Result result = await sender.Send(new UpdateManufacturer.Command(id, request.Name));
        return result.MapResult();
    }

    private record UpdateManufacturerRequest(string Name);
}
