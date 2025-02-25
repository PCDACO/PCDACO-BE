using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Bookings;

public class ExpireBookingsJob(IAppDBContext context)
{
    public async Task ExpireOldBookings()
    {
        // Get the expired status ID
        var expiredStatusId = await context
            .BookingStatuses.Where(s => s.Name == BookingStatusEnum.Expired.ToString())
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (expiredStatusId == Guid.Empty)
            return; // No expired status found

        // Update the status of expired bookings directly
        var affectedRows = await context
            .Bookings.Where(b =>
                b.Status.Name == BookingStatusEnum.Pending.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-30)
            )
            .Where(b =>
                b.Status.Name == BookingStatusEnum.Approved.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-1)
            )
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.StatusId, expiredStatusId));
    }
}
