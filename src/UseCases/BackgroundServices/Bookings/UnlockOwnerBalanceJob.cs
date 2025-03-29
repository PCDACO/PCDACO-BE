using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Bookings;

public class UnlockOwnerBalanceJob(IAppDBContext context)
{
    public async Task UnlockBalance(Guid bookingId)
    {
        var booking = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return;

        // Only unlock if:
        // 1. Booking is completed
        // 2. Past cancellation period (3 days before start)
        // 3. More than half of booking duration has passed (no early return refund possible)
        if (
            booking.Status == BookingStatusEnum.Completed
            || (booking.StartTime - DateTimeOffset.UtcNow).TotalDays <= 3
            || (DateTimeOffset.UtcNow - booking.StartTime).TotalDays
                > Math.Ceiling((booking.EndTime - booking.StartTime).TotalDays) / 2
        )
        {
            var ownerEarningAmount = booking.BasePrice;
            booking.Car.Owner.LockedBalance = Math.Max(
                0,
                booking.Car.Owner.LockedBalance - ownerEarningAmount
            );
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }
}
