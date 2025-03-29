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

        // Only unlock if the booking is completed or past cancellation period
        if (
            booking.Status == BookingStatusEnum.Completed
            || (booking.StartTime - DateTimeOffset.UtcNow).TotalDays <= 3
        ) // Using 3 days as minimum refund period
        {
            var ownerEarningAmount = booking.BasePrice;
            booking.Car.Owner.LockedBalance = Math.Max(0, booking.Car.Owner.LockedBalance - ownerEarningAmount);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }
}
