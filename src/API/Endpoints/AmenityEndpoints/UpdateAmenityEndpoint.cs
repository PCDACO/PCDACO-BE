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
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromForm] UpdateAmenityRequest request
    )
    {
        Result result = await sender.Send(
            new UpdateAmenity.Command(
                id,
                request.Name,
                request.Description,
                request.Icon?.OpenReadStream()
            )
        );
        return result.MapResult();
    }

    private record UpdateAmenityRequest(string Name, string Description, IFormFile? Icon);
}
