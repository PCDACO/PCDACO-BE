using MediatR;
using Microsoft.AspNetCore.SignalR;
using UseCases.UC_Car.Commands;
using UseCases.UC_Car.Queries;

namespace UseCases.Services.SignalR;

public class LocationHub(ISender mediator) : Hub
{
    public async Task SendLocationUpdate(Guid carId, decimal latitude, decimal longitude)
    {
        // Save to database and broadcast to other clients using the existing command
        var result = await mediator.Send(new TrackCarLocation.Command(carId, latitude, longitude));

        if (!result.IsSuccess)
        {
            // If there's an error, send it back to the caller
            await Clients.Caller.SendAsync("LocationUpdateError", result.Errors);
        }
    }

    public async Task GetCarLocation(Guid carId)
    {
        var result = await mediator.Send(new GetCurrentLocationByCarId.Query(carId));

        if (result.IsSuccess)
        {
            await Clients.Caller.SendAsync("ReceiveCarLocation", result.Value);
        }
        else
        {
            await Clients.Caller.SendAsync("CarLocationError", result.Errors);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
