using Microsoft.AspNetCore.SignalR;

namespace UseCases.Services.SignalR;

public class ScheduleHub : Hub
{
    /// <summary>
    /// this method is called by the client to update the status of a schedule
    /// </summary>
    public async Task UpdateScheduleStatus(Guid scheduleId, int newStatus)
    {
        await Clients.All.SendAsync("ReceiveScheduleUpdate", scheduleId, newStatus);
    }
}
