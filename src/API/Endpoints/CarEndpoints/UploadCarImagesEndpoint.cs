using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Car.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UploadCarImagesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/cars/{carId}/car-images", Handle)
            .WithSummary("Upload car images (the pictures of a cars)")
            .WithTags("Cars")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid carId,
        IFormFileCollection images
        )
    {
        Stream[] carStreams = [.. images.Select(i => i.OpenReadStream())];
        Result<UploadCarImages.Response> result = await sender.Send(new UploadCarImages.Command(carId, carStreams));
        return result.MapResult();
    }

}