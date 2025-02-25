using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Bookings;

public class BookingExpiredJob(IAppDBContext context)
{
    public async Task ExpireOldBookings()
    {
        var expiredStatusId = await context
            .BookingStatuses.Where(s => s.Name == BookingStatusEnum.Expired.ToString())
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (expiredStatusId == Guid.Empty)
            return;

        await context
            .Bookings.Where(b =>
                b.Status.Name == BookingStatusEnum.Pending.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-30)
            )
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.StatusId, expiredStatusId));

        await context
            .Bookings.Where(b =>
                b.Status.Name == BookingStatusEnum.Approved.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-1)
            )
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.StatusId, expiredStatusId));
    }
}
