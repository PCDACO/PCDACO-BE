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
using Transaction = Domain.Entities.Transaction;

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

            var paymentExists = await context.Transactions.AnyAsync(
                t => t.BookingId == bookingId && t.Type.Name == TransactionTypeNames.BookingPayment,
                cancellationToken
            );

            if (paymentExists)
                return Result.Conflict("Giao dịch đã được xử lý trước đó");

            var transactionTypes = await context
                .TransactionTypes.Where(t =>
                    new[]
                    {
                        TransactionTypeNames.BookingPayment,
                        TransactionTypeNames.PlatformFee,
                        TransactionTypeNames.OwnerEarning
                    }.Contains(t.Name)
                )
                .ToListAsync(cancellationToken);

            if (transactionTypes.Count != 3)
                return Result.Error("Loại giao dịch không hợp lệ");

            var transactionStatus = await context.TransactionStatuses.FirstOrDefaultAsync(
                t => t.Name == TransactionStatusNames.Completed,
                cancellationToken
            );

            if (transactionStatus == null)
                return Result.Error("Trạng thái giao dịch không hợp lệ");

            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
                return Result.Error("Không tìm thấy admin");

            GeneratePaymentTransactions(
                booking,
                transactionTypes,
                transactionStatus,
                admin,
                out Transaction bookingPayment,
                out Transaction platformFeeTransaction,
                out Transaction ownerEarningTransaction
            );

            admin.Balance += booking.PlatformFee;
            booking.Car.Owner.Balance += ownerEarningTransaction.Amount;

            // Update booking and statistics
            booking.IsPaid = true;

            context.Transactions.AddRange(
                bookingPayment,
                platformFeeTransaction,
                ownerEarningTransaction
            );
            await context.SaveChangesAsync(cancellationToken);

            BackgroundJob.Enqueue(
                () =>
                    SendEmail(
                        webhookData.amount,
                        booking.BasePrice,
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name
                    )
            );

            return Result.Success();
        }

        private static void GeneratePaymentTransactions(
            Booking booking,
            List<TransactionType> transactionTypes,
            TransactionStatus transactionStatus,
            User admin,
            out Transaction bookingPayment,
            out Transaction platformFeeTransaction,
            out Transaction ownerEarningTransaction
        )
        {
            // Create transaction for booking payment.
            bookingPayment = new Transaction
            {
                FromUserId = booking.UserId,
                ToUserId = admin.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionTypes
                    .First(t => t.Name == TransactionTypeNames.BookingPayment)
                    .Id,
                StatusId = transactionStatus.Id,
                Amount = booking.TotalAmount
            };

            // Create transaction for platform (admin) earning.
            platformFeeTransaction = new Transaction
            {
                FromUserId = admin.Id, // funds originate from admin's wallet
                ToUserId = admin.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionTypes.First(t => t.Name == TransactionTypeNames.PlatformFee).Id,
                StatusId = transactionStatus.Id,
                Amount = booking.PlatformFee,
            };

            // Create transaction for owner earning.
            ownerEarningTransaction = new Transaction
            {
                FromUserId = admin.Id, // funds are transferred from the platform
                ToUserId = booking.Car.Owner.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionTypes
                    .First(t => t.Name == TransactionTypeNames.OwnerEarning)
                    .Id,
                StatusId = transactionStatus.Id,
                Amount = booking.BasePrice,
            };
        }

        public async Task SendEmail(
            int amount,
            decimal ownerEarning,
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
                "Xác Nhận Thanh Toán Đặt Xe",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerPaymentConfirmedTemplate.Template(
                ownerName,
                driverName,
                carModel,
                amount,
                ownerEarning,
                DateTimeOffset.UtcNow
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo: Có Yêu Cầu Đặt Xe Mới",
                ownerEmailTemplate
            );
        }
    }
}
