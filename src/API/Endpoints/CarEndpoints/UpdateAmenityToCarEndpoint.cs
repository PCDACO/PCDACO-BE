using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Car.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UpdateAmenityToCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/cars/{id:guid}/amenities", Handle)
            .WithSummary("Update amenity to car")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, UpdateAmenityToCarRequest request)
    {
        Result result = await sender.Send(new UpdateAmenityToCar.Command(
            CarId: id,
            AmenityId: request.AmenityId
        ));
        return result.MapResult();
    }
    private record UpdateAmenityToCarRequest(
        Guid[] AmenityId
    );
}