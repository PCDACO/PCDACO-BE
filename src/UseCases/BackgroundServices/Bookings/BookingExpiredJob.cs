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
            .Bookings.Include(b => b.Status)
            .Include(b => b.Car)
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
            // Mark as expired
            booking.Status = BookingStatusEnum.Expired;
            booking.Note = "Hết hạn tự động do không nhận xe đúng hạn";

            if (booking.IsPaid)
            {
                booking.IsRefund = true;
                booking.RefundAmount = booking.TotalAmount * HALF_REFUND_PERCENTAGE;

                var adminAmount = booking.RefundAmount * ADMIN_REFUND_PERCENTAGE;
                var ownerAmount = booking.RefundAmount * OWNER_REFUND_PERCENTAGE;

                admin.Balance -= (decimal)adminAmount;
                booking.Car.Owner.Balance -= (decimal)ownerAmount;
                booking.User.Balance += (decimal)booking.RefundAmount;
            }

            await SendExpirationEmail(booking, booking.IsPaid);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task ExpirePendingOverDateBookings()
    {
        // Find all ReadyForPickup bookings that started more than 24 hours ago
        var expiredBookings = await context
            .Bookings.Include(b => b.Status)
            .Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
            .Where(b =>
                b.Status == BookingStatusEnum.Pending
                && b.StartTime < DateTimeOffset.UtcNow
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
            // Mark as expired
            booking.Status = BookingStatusEnum.Expired;
            booking.Note = "Hết hạn tự động do không nhận xe đúng hạn";

            if (booking.IsPaid)
            {
                booking.IsRefund = true;
                booking.RefundAmount = booking.TotalAmount * FULL_REFUND_PERCENTAGE;

                var adminAmount = booking.RefundAmount * ADMIN_REFUND_PERCENTAGE;
                var ownerAmount = booking.RefundAmount * OWNER_REFUND_PERCENTAGE;

                admin.Balance -= (decimal)adminAmount;
                booking.Car.Owner.Balance -= (decimal)ownerAmount;
                booking.User.Balance += (decimal)booking.RefundAmount;
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
}