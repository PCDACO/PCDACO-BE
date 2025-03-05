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
    private const decimal REFUND_PERCENTAGE = 0.5m;

    public async Task ExpireBookingsAutomatically()
    {
        await ExpireReadyForPickupBookings();

        await UpdateCarsToAvailable();
    }

    private async Task ExpireReadyForPickupBookings()
    {
        var expiredStatus = await context.BookingStatuses.FirstOrDefaultAsync(s =>
            s.Name == BookingStatusEnum.Expired.ToString()
        );

        if (expiredStatus == null)
            return;

        // Find all ReadyForPickup bookings that started more than 24 hours ago
        var expiredBookings = await context
            .Bookings.Include(b => b.Status)
            .Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
            .Where(b =>
                b.Status.Name == BookingStatusEnum.ReadyForPickup.ToString()
                && b.StartTime < DateTimeOffset.UtcNow.AddHours(-24)
            )
            .ToListAsync();

        if (!expiredBookings.Any())
            return;

        foreach (var booking in expiredBookings)
        {
            // Mark as expired
            booking.StatusId = expiredStatus.Id;
            booking.Note = "Hết hạn tự động do không nhận xe đúng hạn";

            booking.IsRefund = true;
            booking.RefundAmount = booking.TotalAmount * REFUND_PERCENTAGE;

            // Send notification email
            await SendExpirationEmail(booking);
        }

        // Save all changes
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SendExpirationEmail(Booking booking)
    {
        // TODO: add refund amount to the email template
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
                    "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn - Hoàn trả 50% tiền đặt cọc",
                    driverEmailTemplate
                )
        );
    }

    private async Task UpdateCarsToAvailable()
    {
        var availableStatusId = await context
            .CarStatuses.Where(c => c.Name == CarStatusNames.Available)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (availableStatusId == Guid.Empty)
            return;

        // Find all car IDs associated with expired bookings
        var expiredCarIds = await context
            .Bookings.Where(b => b.Status.Name == BookingStatusEnum.Expired.ToString())
            .Select(b => b.CarId)
            .Distinct()
            .ToListAsync();

        if (expiredCarIds.Count == 0)
            return;

        await context
            .Cars.Where(c => expiredCarIds.Contains(c.Id))
            .ExecuteUpdateAsync(c => c.SetProperty(car => car.StatusId, availableStatusId));
    }
}
