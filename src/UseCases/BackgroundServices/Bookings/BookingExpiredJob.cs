using Domain.Enums;
using Hangfire;
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

        // Use Hangfire retries to ensure failed expiration jobs run again
        BackgroundJob.Enqueue(() => ExpirePendingBookings(expiredStatusId));
        BackgroundJob.Enqueue(() => ExpireApprovedBookings(expiredStatusId));
    }

    public async Task ExpirePendingBookings(Guid expiredStatusId)
    {
        await context
            .Bookings.Where(b =>
                b.Status.Name == BookingStatusEnum.Pending.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-30)
            )
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.StatusId, expiredStatusId));
    }

    public async Task ExpireApprovedBookings(Guid expiredStatusId)
    {
        await context
            .Bookings.Where(b =>
                b.Status.Name == BookingStatusEnum.Approved.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddDays(-1)
            )
            .ExecuteUpdateAsync(b => b.SetProperty(b => b.StatusId, expiredStatusId));
    }
}
