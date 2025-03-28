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
    const decimal ADMIN_REFUND_PERCENTAGE = 0.1m;
    const decimal OWNER_REFUND_PERCENTAGE = 0.9m;

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

    private async Task SendExpirationEmail(Booking booking, bool isPaid)
    {
        await Task.Delay(0);
        // TODO: add refund amount to the email template
        var driverEmailTemplate = DriverExpiredBookingTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount
        );

        string message = isPaid
            ? "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn  - Hoàn trả 50% tiền đặt cọc"
            : "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn";

        BackgroundJob.Enqueue(
            () => emailService.SendEmailAsync(booking.User.Email, message, driverEmailTemplate)
        );
    }

    private async Task UpdateCarsToAvailable()
    {
        // Find all car IDs associated with expired bookings
        var expiredCarIds = await context
            .Bookings.Where(b => b.Status == BookingStatusEnum.Expired)
            .Select(b => b.CarId)
            .Distinct()
            .ToListAsync();

        if (expiredCarIds.Count() == 0)
            return;

        await context
            .Cars.Where(c => expiredCarIds.Contains(c.Id))
            .ExecuteUpdateAsync(c => c.SetProperty(car => car.Status, CarStatusEnum.Available));
    }

    public async Task ExpireUnpaidApprovedBooking(Guid bookingId)
    {
        var booking = await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
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

        await context.SaveChangesAsync(CancellationToken.None);

        var driverEmailTemplate = DriverExpiredBookingTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount
        );

        BackgroundJob.Enqueue(
            () =>
                emailService.SendEmailAsync(
                    booking.User.Email,
                    "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn do không thanh toán trong vòng 12 giờ",
                    driverEmailTemplate
                )
        );
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

        var refundAmount = booking.TotalAmount * refundPercentage;
        var adminRefundAmount = refundAmount * ADMIN_REFUND_PERCENTAGE;
        var ownerRefundAmount = refundAmount * OWNER_REFUND_PERCENTAGE;

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
            BalanceAfter = booking.User.Balance + refundAmount
        };

        // Update balances
        admin.Balance -= adminRefundAmount;
        booking.Car.Owner.Balance -= ownerRefundAmount;
        booking.User.Balance += refundAmount;

        // Update booking refund info
        booking.IsRefund = true;
        booking.RefundAmount = refundAmount;
        booking.RefundDate = DateTimeOffset.UtcNow;

        context.Transactions.AddRange(adminRefundTransaction, ownerRefundTransaction);
    }
}
