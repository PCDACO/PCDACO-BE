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

public class BookingOverdueJob(IAppDBContext context, IEmailService emailService)
{
    private const int PRE_CANCELLATION_HOURS = 6;

    public async Task HandleOverdueBookings()
    {
        // Find all ongoing bookings that are overdue and affecting next bookings
        var overdueBookings = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Model)
            .Include(b => b.User)
            .Where(b =>
                b.Status == BookingStatusEnum.Ongoing
                && !b.IsCarReturned
                && b.EndTime < DateTimeOffset.UtcNow
            )
            .ToListAsync();

        if (!overdueBookings.Any())
            return;

        foreach (var overdueBooking in overdueBookings)
        {
            // Find the next affected booking for this car
            var affectedBooking = await context
                .Bookings.Include(b => b.User)
                .Include(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.BookingLockedBalances)
                .FirstOrDefaultAsync(b =>
                    b.CarId == overdueBooking.CarId
                    && b.Status == BookingStatusEnum.Approved
                    && b.StartTime <= DateTimeOffset.UtcNow.AddHours(PRE_CANCELLATION_HOURS)
                    && b.StartTime > overdueBooking.EndTime
                );

            if (affectedBooking == null)
                continue;

            // Update affected booking status
            affectedBooking.Status = BookingStatusEnum.Cancelled;
            affectedBooking.Note =
                $"Hủy tự động do chuyến đi trước (ID: {overdueBooking.Id}) chưa trả xe";

            if (affectedBooking.IsPaid)
            {
                var admin = await context.Users.FirstOrDefaultAsync(u =>
                    u.Role.Name == UserRoleNames.Admin
                );

                if (admin != null)
                {
                    await HandleBookingRefund(
                        affectedBooking,
                        admin,
                        "Hoàn tiền do chuyến đi trước chưa trả xe"
                    );
                }
            }

            // Send notifications
            await SendOverdueNotification(overdueBooking);
            await SendAffectedBookingNotification(affectedBooking);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task HandleBookingRefund(Booking booking, User admin, string reason)
    {
        var adminRefundAmount = booking.PlatformFee;
        var ownerRefundAmount = booking.BasePrice;
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

    private async Task SendOverdueNotification(Booking booking)
    {
        var template = DriverBookingOverdueTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.EndTime,
            DateTimeOffset.UtcNow
        );

        BackgroundJob.Enqueue(
            () =>
                emailService.SendEmailAsync(
                    booking.User.Email,
                    "Cảnh báo: Xe chưa được trả đúng hạn",
                    template
                )
        );
    }

    private async Task SendAffectedBookingNotification(Booking booking)
    {
        var template = DriverBookingCancelledDueToOverdueTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime
        );

        BackgroundJob.Enqueue(
            () =>
                emailService.SendEmailAsync(
                    booking.User.Email,
                    "Thông báo: Đơn đặt xe bị hủy do chuyến trước chưa trả xe",
                    template
                )
        );
    }
}
