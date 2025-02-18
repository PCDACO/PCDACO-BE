using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_GPSDevice.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class CreateGPSDeviceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/gps-devices/", Handle)
            .WithSummary("Create GPS Device")
            .WithTags("GPS Devices")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        CreateGPSDeviceRequest request
    )
    {
        Result<CreateGPSDevice.Response> result = await sender.Send(new CreateGPSDevice.Command(request.Name), default);
        return result.MapResult();
    }
    private record CreateGPSDeviceRequest(string Name);
}