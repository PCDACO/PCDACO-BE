using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
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

            // TODO: Add transaction record

            var transactionType = context
                .TransactionTypes.Where(t =>
                    new List<string>
                    {
                        TransactionTypeNames.BookingPayment,
                        TransactionTypeNames.PlatformFee
                    }.Any(name => EF.Functions.ILike(t.Name, name))
                )
                .ToList();

            if (transactionType.Count != 2)
                return Result.Error("Loại giao dịch không hợp lệ");

            var transactionStatus = await context.TransactionStatuses.FirstOrDefaultAsync(
                t => t.Name == TransactionStatusNames.Completed,
                cancellationToken: cancellationToken
            );

            if (transactionStatus == null)
                return Result.Error("Trạng thái giao dịch không hợp lệ");

            var bookingPayment = new Domain.Entities.Transaction
            {
                FromUserId = booking.User.Id,
                ToUserId = booking.Car.Owner.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionType
                    .First(t => t.Name == TransactionTypeNames.BookingPayment)
                    .Id,
                StatusId = transactionStatus.Id,
                Amount = webhookData.amount,
            };

            var platformFee = new Domain.Entities.Transaction
            {
                FromUserId = booking.User.Id,
                ToUserId = booking.Car.Owner.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionType.First(t => t.Name == TransactionTypeNames.PlatformFee).Id,
                StatusId = transactionStatus.Id,
                Amount = booking.PlatformFee,
            };

            // Update booking and statistics
            booking.IsPaid = true;
            booking.Car.Owner.UserStatistic.TotalEarning += webhookData.amount;
            booking.Car.CarStatistic.TotalEarning += webhookData.amount;

            await context.SaveChangesAsync(cancellationToken);

            decimal ownerAmount = webhookData.amount - booking.PlatformFee;

            BackgroundJob.Enqueue(
                () =>
                    SendEmail(
                        webhookData.amount,
                        ownerAmount,
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name
                    )
            );

            return Result.Success();
        }

        public async Task SendEmail(
            int amount,
            decimal ownerEraning,
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carModel
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverPaymentConfirmedTemplate.Template(
                driverName,
                carModel,
                amount,
                DateTimeOffset.UtcNow
            );

            await emailService.SendEmailAsync(
                driverEmail,
                "Xác Nhận Thanh Toán",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerPaymentConfirmedTemplate.Template(
                ownerName,
                driverName,
                carModel,
                amount,
                ownerEraning,
                DateTimeOffset.UtcNow
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo Thanh Toán Thành Công",
                ownerEmailTemplate
            );
        }
    }
}
