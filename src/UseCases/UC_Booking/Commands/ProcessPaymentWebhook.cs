using Ardalis.Result;
using Domain.Shared.EmailTemplates.EmailBookings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Net.payOS.Types;
using UseCases.Abstractions;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public sealed class ProcessPaymentWebhook
{
    public sealed record Command(WebhookType WebhookType) : IRequest<Result>;

    public class Handler(IAppDBContext context, IEmailService emailService)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify webhook data
            WebhookData webhookData = request.WebhookType.data;

            if (webhookData == null)
                return Result.Error("Dữ liệu webhook không hợp lệ");

            // Extract bookingId from signature
            if (!Guid.TryParse(request.WebhookType.signature, out Guid bookingId))
                return Result.Error("BookingId không hợp lệ");

            // Get booking details with related data
            var booking = await context
                .Bookings.Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            // Update booking and statistics
            booking.IsPaid = true;
            booking.Car.Owner.UserStatistic.TotalEarning += webhookData.amount;
            booking.Car.CarStatistic.TotalEarning += webhookData.amount;

            await context.SaveChangesAsync(cancellationToken);

            decimal ownerAmount = webhookData.amount - booking.PlatformFee;

            await SendEmail(webhookData, booking, ownerAmount);

            return Result.Success();
        }

        private async Task SendEmail(
            WebhookData webhookData,
            Domain.Entities.Booking booking,
            decimal ownerAmount
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverPaymentConfirmedTemplate.Template(
                booking.User.Name,
                booking.Car.Model.Name,
                webhookData.amount,
                DateTimeOffset.UtcNow
            );

            await emailService.SendEmailAsync(
                booking.User.Email,
                "Xác Nhận Thanh Toán",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerPaymentConfirmedTemplate.Template(
                booking.Car.Owner.Name,
                booking.User.Name,
                booking.Car.Model.Name,
                webhookData.amount,
                ownerAmount,
                DateTimeOffset.UtcNow
            );

            await emailService.SendEmailAsync(
                booking.Car.Owner.Email,
                "Thông Báo Thanh Toán Thành Công",
                ownerEmailTemplate
            );
        }
    }
}
