using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

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
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, CreateAmenityRequest request)
    {
        Result<CreateAmenity.Response> result = await sender.Send(new CreateAmenity.Command(request.Name, request.Description));
        return result.MapResult();
    }
    private record CreateAmenityRequest(
        string Name,
        string Description
    );
}