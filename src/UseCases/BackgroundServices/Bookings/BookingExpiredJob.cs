using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.EmailService;

namespace UseCases.BackgroundServices.Bookings;

public class BookingExpiredJob(
    IAppDBContext context,
    IEmailService emailService,
    IBackgroundJobClient backgroundJobClient
)
{
    private const int AUTO_EXPIRE_APPROVED_HOURS = 24;

    public async Task ExpireOldBookings(Guid bookingId)
    {
        backgroundJobClient.Schedule(
            () => ExpireApprovedBookings(bookingId),
            TimeSpan.FromHours(AUTO_EXPIRE_APPROVED_HOURS)
        );

        await UpdateCarsToAvailable();
    }

    public async Task ExpireApprovedBookings(Guid bookingId)
    {
        var booking = await GetBookingIfApproved(bookingId);
        if (booking == null)
            return;

        // Get the expired status
        var expiredStatus = await context.BookingStatuses.FirstOrDefaultAsync(s =>
            s.Name == BookingStatusEnum.Expired.ToString()
        );

        if (expiredStatus == null)
            return;

        booking.StatusId = expiredStatus.Id;
        booking.Note = "Hết hạn tự động do bạn không nhận xe đúng hạn";

        await context.SaveChangesAsync(CancellationToken.None);

        var driverEmailTempalte = DriverExpiredBookingTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount
        );

        // Notify driver about expiration
        await emailService.SendEmailAsync(
            booking.User.Email,
            "Thông báo: Yêu cầu đặt xe của bạn đã hết hạn",
            driverEmailTempalte
        );
    }

    private async Task<Booking?> GetBookingIfApproved(Guid bookingId)
    {
        return await context
            .Bookings.Include(b => b.Status)
            .Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId && b.Status.Name == BookingStatusEnum.Approved.ToString()
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
