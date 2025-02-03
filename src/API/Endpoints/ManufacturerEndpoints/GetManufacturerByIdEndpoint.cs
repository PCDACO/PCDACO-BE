using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_Manufacturer.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public sealed class GetManufacturerByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manufacturers/{id:guid}", Handle)
            .WithSummary("Get a manufacturer by ID")
            .WithTags("Manufacturers")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetManufacturerById.Response> result = await sender.Send(
            new GetManufacturerById.Query(id)
        );
        return result.MapResult();
    }
}
