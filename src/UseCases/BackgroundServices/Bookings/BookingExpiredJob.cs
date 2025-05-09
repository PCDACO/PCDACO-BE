using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.EmailService;

namespace UseCases.BackgroundServices.Bookings;

public class BookingExpiredJob(IAppDBContext context, IEmailService emailService)
{
    private const decimal HALF_REFUND_PERCENTAGE = 0.5m;
    private const decimal FULL_REFUND_PERCENTAGE = 1m;

    public async Task ExpireBookingsAutomatically()
    {
        await ExpireReadyForPickupBookings();

        await ExpirePendingOverDateBookings();

        await UpdateCarsToAvailable();
    }

    private async Task ExpireReadyForPickupBookings()
    {
        // Find all ReadyForPickup bookings that started more than 24 hours ago
        var expiredBookings = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .ThenInclude(o => o.BookingLockedBalances)
            .Include(b => b.User)
            .Where(b =>
                b.Status == BookingStatusEnum.ReadyForPickup
                && b.StartTime < DateTimeOffset.UtcNow.AddHours(-24)
            )
            .ToListAsync();

        if (!expiredBookings.Any())
            return;

        var admin = await context.Users.FirstOrDefaultAsync(u =>
            u.Role.Name == UserRoleNames.Admin
        );

        if (admin == null)
            return;

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatusEnum.Expired;
            booking.Note = "Hết hạn tự động do không nhận xe đúng hạn";

            if (booking.IsPaid)
            {
                await HandleExpiredBookingRefund(
                    booking,
                    admin,
                    HALF_REFUND_PERCENTAGE,
                    "Booking hết hạn do không nhận xe đúng hạn"
                );
            }

            await SendExpirationEmail(booking, booking.IsPaid);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task ExpirePendingOverDateBookings()
    {
        // Find all ReadyForPickup bookings that started more than 24 hours ago
        var expiredBookings = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
            .Where(b =>
                b.Status == BookingStatusEnum.Pending && b.StartTime < DateTimeOffset.UtcNow
            )
            .ToListAsync();

        if (!expiredBookings.Any())
            return;

        var admin = await context.Users.FirstOrDefaultAsync(u =>
            u.Role.Name == UserRoleNames.Admin
        );

        if (admin == null)
            return;

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatusEnum.Expired;
            booking.Note = "Hết hạn tự động do không được phê duyệt";

            if (booking.IsPaid)
            {
                await HandleExpiredBookingRefund(
                    booking,
                    admin,
                    FULL_REFUND_PERCENTAGE,
                    "Booking hết hạn do không được phê duyệt"
                );
            }

            await SendExpirationEmail(booking, booking.IsPaid);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task HandleExpiredBookingRefund(
        Booking booking,
        User admin,
        decimal refundPercentage,
        string reason
    )
    {
        if (!booking.IsPaid)
            return;

        var adminRefundAmount = booking.PlatformFee * refundPercentage;
        var ownerRefundAmount = booking.BasePrice * refundPercentage;
        var refundAmount = adminRefundAmount + ownerRefundAmount;

        var refundType = await context.TransactionTypes.FirstAsync(t =>
            t.Name == TransactionTypeNames.Refund
        );

        var adminRefundTransaction = new Transaction
        {
            FromUserId = admin.Id,
            ToUserId = booking.UserId,
            BookingId = booking.Id,
            BankAccountId = null,
            TypeId = refundType.Id,
            Status = TransactionStatusEnum.Completed,
            Amount = adminRefundAmount,
            Description = $"Hoàn tiền từ Admin: {reason}",
            BalanceAfter = booking.User.Balance + adminRefundAmount
        };

        var ownerRefundTransaction = new Transaction
        {
            FromUserId = booking.Car.OwnerId,
            ToUserId = booking.UserId,
            BookingId = booking.Id,
            BankAccountId = null,
            TypeId = refundType.Id,
            Status = TransactionStatusEnum.Completed,
            Amount = ownerRefundAmount,
            Description = $"Hoàn tiền từ Chủ xe: {reason}",
            BalanceAfter = booking.User.Balance + ownerRefundAmount
        };

        // Find the booking's locked balance
        var bookingLockedBalance = booking.Car.Owner.BookingLockedBalances.FirstOrDefault(b =>
            b.BookingId == booking.Id
        );

        if (bookingLockedBalance != null)
        {
            if (bookingLockedBalance.Amount >= ownerRefundAmount)
            {
                // Use booking's locked balance for refund
                bookingLockedBalance.Amount -= ownerRefundAmount;
                booking.Car.Owner.LockedBalance -= ownerRefundAmount;

                // If there's remaining locked balance, move it to available balance
                if (bookingLockedBalance.Amount > 0)
                {
                    booking.Car.Owner.Balance += bookingLockedBalance.Amount;
                    booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                    bookingLockedBalance.Amount = 0;
                }
            }
            else
            {
                // If booking's locked balance is insufficient, use all of it and take the rest from available balance
                var remainingRefund = ownerRefundAmount - bookingLockedBalance.Amount;
                booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                booking.Car.Owner.Balance -= remainingRefund;
                bookingLockedBalance.Amount = 0;
            }
        }
        else
        {
            // If no locked balance record found, take from available balance
            booking.Car.Owner.Balance -= ownerRefundAmount;
        }

        // Update balances
        admin.Balance -= adminRefundAmount;
        booking.User.Balance += refundAmount;

        // Update booking refund info
        booking.IsRefund = true;
        booking.RefundAmount = refundAmount;
        booking.RefundDate = DateTimeOffset.UtcNow;

        context.Transactions.AddRange(adminRefundTransaction, ownerRefundTransaction);
    }

    public async Task ExpireUnpaidApprovedBooking(Guid bookingId)
    {
        var booking = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .ThenInclude(o => o.BookingLockedBalances)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId
                && (
                    b.Status == BookingStatusEnum.Approved
                    || b.Status == BookingStatusEnum.ReadyForPickup
                )
                && !b.IsPaid
            );

        if (booking == null)
            return;

        // Mark as expired
        booking.Status = BookingStatusEnum.Expired;
        booking.Note =
            "Hết hạn tự động do không thanh toán trong vòng 12 giờ sau khi được phê duyệt";
        booking.Car.Status = CarStatusEnum.Available;

        // Find and clear any locked balance for this booking
        var bookingLockedBalance = booking.Car.Owner.BookingLockedBalances.FirstOrDefault(b =>
            b.BookingId == booking.Id
        );

        if (bookingLockedBalance != null)
        {
            booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
            bookingLockedBalance.Amount = 0;
        }

        await context.SaveChangesAsync(CancellationToken.None);

        await SendExpirationEmail(booking, booking.IsPaid);
    }

    private async Task UpdateCarsToAvailable()
    {
        // Find all car IDs associated with expired bookings
        var expiredBookings = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Where(b => b.Status == BookingStatusEnum.Expired)
            .ToListAsync();

        if (!expiredBookings.Any())
            return;

        foreach (var booking in expiredBookings)
        {
            booking.Car.Status = CarStatusEnum.Available;

            // If the booking wasn't paid, ensure any locked balance is released
            if (!booking.IsPaid && booking.Car.Owner.LockedBalance > 0)
            {
                var lockedAmountForBooking = booking.BasePrice;
                booking.Car.Owner.LockedBalance = Math.Max(
                    0,
                    booking.Car.Owner.LockedBalance - lockedAmountForBooking
                );
            }
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SendExpirationEmail(Booking booking, bool isPaid)
    {
        var driverEmailTemplate = DriverExpiredBookingTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount
        );

        string message;
        if (isPaid)
        {
            var refundPercentage =
                booking.Status == BookingStatusEnum.ReadyForPickup
                    ? HALF_REFUND_PERCENTAGE
                    : FULL_REFUND_PERCENTAGE;
            message =
                $"Thông báo: Yêu cầu đặt xe của bạn đã hết hạn - Hoàn trả {refundPercentage * 100}% tiền đặt cọc";
        }
        else
        {
            message = "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn";
        }

        BackgroundJob.Enqueue(
            () => emailService.SendEmailAsync(booking.User.Email, message, driverEmailTemplate)
        );
    }
}
