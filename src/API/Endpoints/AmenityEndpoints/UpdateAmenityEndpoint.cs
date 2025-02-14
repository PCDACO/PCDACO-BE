using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.UC_Amenity.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AmenityEndpoints;

public class UpdateAmenityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/amenities/{id:guid}", Handle)
            .WithSummary("Update an amenity")
            .WithTags("Amenities")
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromForm] string name,
        [FromForm] string description,
        IFormFile? icon
    )
    {
        Result<UpdateAmenity.Response> result = await sender.Send(
            new UpdateAmenity.Command(
                id,
                name,
                description,
                icon != null ? icon.OpenReadStream() : null!
            )
        );
        return result.MapResult();
    }
}
