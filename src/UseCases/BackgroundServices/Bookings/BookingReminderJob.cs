using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.EmailService;
using UUIDNext.Tools;

namespace UseCases.BackgroundServices.Bookings;

public class BookingReminderJob(
    IAppDBContext context,
    IEmailService emailService,
    IBackgroundJobClient backgroundJobClient
)
{
    private const int FIRST_REMINDER_HOURS = 12;
    private const int FINAL_REMINDER_HOURS = 20;
    private const int AUTO_EXPIRE_HOURS = 24;

    public async Task ScheduleReminders(Guid bookingId)
    {
        await Task.Delay(0);
        backgroundJobClient.Schedule(
            () => SendFirstReminder(bookingId),
            TimeSpan.FromHours(FIRST_REMINDER_HOURS)
        );

        backgroundJobClient.Schedule(
            () => SendFinalReminder(bookingId),
            TimeSpan.FromHours(FINAL_REMINDER_HOURS)
        );

        backgroundJobClient.Schedule(
            () => AutoExpireBooking(bookingId),
            TimeSpan.FromHours(AUTO_EXPIRE_HOURS)
        );
    }

    public async Task SendFirstReminder(Guid bookingId)
    {
        var booking = await GetBookingIfPending(bookingId);
        if (booking == null)
            return;

        UuidDecoder.TryDecodeTimestamp(booking.Id, out DateTime bookingCreatedTime);

        var ownerEmailTemplate = OwnerBookingReminderTemplate.Template(
            booking.Car.Owner.Name,
            booking.User.Name,
            booking.Car.Model.Name,
            bookingCreatedTime,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount,
            1
        );

        await emailService.SendEmailAsync(
            booking.Car.Owner.Email,
            "Nhắc nhở: Bạn có yêu cầu đặt xe đang chờ phản hồi",
            ownerEmailTemplate
        );
    }

    public async Task SendFinalReminder(Guid bookingId)
    {
        var booking = await GetBookingIfPending(bookingId);
        if (booking == null)
            return;

        UuidDecoder.TryDecodeTimestamp(booking.Id, out DateTime bookingCreatedTime);

        var ownerEmailTemplate = OwnerBookingReminderTemplate.Template(
            booking.Car.Owner.Name,
            booking.User.Name,
            booking.Car.Model.Name,
            bookingCreatedTime,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount,
            2
        );

        await emailService.SendEmailAsync(
            booking.Car.Owner.Email,
            "CẢNH BÁO CUỐI: Đặt xe sẽ bị hủy tự động",
            ownerEmailTemplate
        );
    }

    public async Task AutoExpireBooking(Guid bookingId)
    {
        var booking = await GetBookingIfPending(bookingId);
        if (booking == null)
            return;

        var admin = await context.Users.FirstOrDefaultAsync(u =>
            u.Role.Name == UserRoleNames.Admin
        );
        if (admin == null)
            return;

        booking.Status = BookingStatusEnum.Expired;
        booking.Note = "Hết hạn tự động do chủ xe không phản hồi";

        if (booking.IsPaid)
        {
            var refundType = await context.TransactionTypes.FirstAsync(t =>
                t.Name == TransactionTypeNames.Refund
            );

            var refundTransaction = new Transaction
            {
                FromUserId = admin.Id,
                ToUserId = booking.UserId,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = refundType.Id,
                Status = TransactionStatusEnum.Completed,
                Amount = booking.TotalAmount, // Full refund
                Description = "Hoàn tiền do chủ xe không phản hồi",
                BalanceAfter = booking.User.Balance + booking.TotalAmount
            };

            booking.IsRefund = true;
            booking.RefundAmount = booking.TotalAmount;
            booking.RefundDate = DateTimeOffset.UtcNow;

            booking.User.Balance += booking.TotalAmount;
            admin.Balance -= booking.TotalAmount;

            context.Transactions.Add(refundTransaction);
        }

        await context.SaveChangesAsync(CancellationToken.None);

        // TODO: include refund amout in the email template
        var driverEmailTemplate = DriverExpiredBookingTemplate.Template(
            booking.User.Name,
            booking.Car.Model.Name,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmount
        );

        string message = booking.IsPaid
            ? "Yêu cầu đặt xe của bạn đã hết hạn - Hoàn trả 100% tiền đặt cọc"
            : "Yêu cầu đặt xe của bạn đã hết hạn";

        // Notify driver about expiration and refund
        await emailService.SendEmailAsync(booking.User.Email, message, driverEmailTemplate);
    }

    private async Task<Booking?> GetBookingIfPending(Guid bookingId)
    {
        return await context
            .Bookings.Include(b => b.Car)
            .ThenInclude(c => c.Owner)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Status == BookingStatusEnum.Pending);
    }
}
