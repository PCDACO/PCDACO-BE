using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Amenity.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AmenityEndpoints;

public class DeleteAmenityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/amenities/{id:guid}", Handle)
            .WithSummary("Delete an amenity")
            .WithTags("Amenities")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id
    )
    {
        Result result = await sender.Send(new DeleteAmenity.Command(id));
        return result.MapResult();
    }
}