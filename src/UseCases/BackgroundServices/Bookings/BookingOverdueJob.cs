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
    private const decimal COMPENSATION_PERCENTAGE = 0.3m; // 30% compensation of the affected booking's total amount
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
                .FirstOrDefaultAsync(b =>
                    b.CarId == overdueBooking.CarId
                    && b.Status == BookingStatusEnum.Approved
                    && b.StartTime <= DateTimeOffset.UtcNow.AddHours(PRE_CANCELLATION_HOURS)
                    && b.StartTime > overdueBooking.EndTime
                );

            if (affectedBooking == null)
                continue;

            // Calculate compensation for the affected booking
            decimal compensationAmount = affectedBooking.TotalAmount * COMPENSATION_PERCENTAGE;

            // Update affected booking status
            affectedBooking.Status = BookingStatusEnum.Cancelled;
            affectedBooking.Note =
                $"Hủy tự động do chuyến đi trước (ID: {overdueBooking.Id}) chưa trả xe";

            if (affectedBooking.IsPaid)
            {
                affectedBooking.IsRefund = true;
                affectedBooking.RefundAmount = affectedBooking.TotalAmount;
                affectedBooking.RefundDate = DateTimeOffset.UtcNow;

                // Return the full amount to the affected driver
                affectedBooking.User.Balance += affectedBooking.TotalAmount;
            }

            // Send notifications
            await SendOverdueNotification(overdueBooking);
            await SendAffectedBookingNotification(affectedBooking, compensationAmount);
        }

        await context.SaveChangesAsync(CancellationToken.None);
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

    private async Task SendAffectedBookingNotification(Booking booking, decimal compensationAmount)
    {
        var template = DriverBookingCancelledDueToOverdueTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            compensationAmount
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
