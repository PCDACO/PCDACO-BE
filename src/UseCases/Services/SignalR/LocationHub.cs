using Microsoft.AspNetCore.SignalR;

namespace UseCases.Services.SignalR;

public class LocationHub : Hub
{
    public async Task SendLocationUpdate(Guid bookingId, decimal latitude, decimal longitude)
    {
        await Clients.All.SendAsync("ReceiveLocationUpdate", bookingId, latitude, longitude);
    }
}
