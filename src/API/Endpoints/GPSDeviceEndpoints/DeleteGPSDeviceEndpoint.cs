using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_GPSDevice.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.GPSDeviceEndpoints;

public class DeleteGPSDeviceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/gps-devices/{id:guid}", Handle)
            .WithSummary("Delete GPS Device")
            .WithTags("GPS Devices")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id
    )
    {
        Result result = await sender.Send(new DeleteGPSDevice.Command(id), default);
        return result.MapResult();
    }
}