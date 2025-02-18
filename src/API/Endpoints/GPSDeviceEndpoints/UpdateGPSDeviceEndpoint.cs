using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_GPSDevice.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class UpdateGPSDeviceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/gps-devices/", Handle)
            .WithSummary("Update GPS Device")
            .WithTags("GPS Devices")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateGPSDeviceRequest request
    )
    {
        Result<UpdateGPSDevice.Response> result = await sender.Send(new UpdateGPSDevice.Command(id, request.Name), default);
        return result.MapResult();
    }

    private record UpdateGPSDeviceRequest(
        string Name
    );
}