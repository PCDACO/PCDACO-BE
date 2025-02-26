using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_GPSDevice.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;


namespace API.Endpoints.CarGpsEndpoints;

public class AssignDeviceToCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars/{id:guid}/assign-device", Handle)
            .WithSummary("Assign Device To Car")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, AssignDeviceToCarRequest request)
    {
        Result result = await sender.Send(new AssignDeviceToCar.Command(
            CarId: id,
            Longtitude: request.Longtitude!.Value,
            Latitude: request.Latitude!.Value,
            DeviceId: request.DeviceId
        ));
        return result.MapResult();
    }
    private record AssignDeviceToCarRequest(
        Guid DeviceId,
        double? Longtitude,
        double? Latitude
    );
}