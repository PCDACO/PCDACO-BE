using API.Utils;

using Ardalis.Result;

using Carter;

using Infrastructure.Idempotency;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.UC_Amenity.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AmenityEndpoints;

public class CreateAmenityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/amenities", Handle)
            .WithSummary("Create an amenity")
            .WithTags("Amenities")
            .AddEndpointFilter<IdempotencyFilter>()
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, [FromForm] CreateAmenityRequest request)
    {
        Result<CreateAmenity.Response> result = await sender.Send(
            new CreateAmenity.Command(
                request.Name,
                request.Description,
                [.. request.Icon.Select(i => i.OpenReadStream())]
            )
        );
        return result.MapResult();
    }

    private record CreateAmenityRequest(string Name, string Description, IFormFileCollection Icon);
}
