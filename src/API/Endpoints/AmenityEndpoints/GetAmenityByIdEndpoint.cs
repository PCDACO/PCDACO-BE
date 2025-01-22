using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Amenity.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AmenityEndpoints;

public sealed class GetAmenityByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/amenities/{id:guid}", Handle)
            .WithSummary("Retrieve a specific amenity by its ID")
            .WithTags("Amenities")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result<GetAmenityById.Response> result = await sender.Send(new GetAmenityById.Query(id));
        return result.MapResult();
    }
}