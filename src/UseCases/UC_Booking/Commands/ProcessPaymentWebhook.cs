using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Net.payOS.Types;
using UseCases.Abstractions;
using UseCases.Services.EmailService;
using Transaction = Domain.Entities.Transaction;

namespace UseCases.UC_Booking.Commands;

public sealed class ProcessPaymentWebhook
{
    public sealed record Command(WebhookType WebhookType) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        IEmailService emailService,
        ILogger<Handler> logger,
        IMemoryCache cache
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify webhook data
            WebhookData webhookData = request.WebhookType.data;

            if (webhookData == null)
            {
                logger.LogWarning("Invalid webhook data received");
                return Result.Error("Dữ liệu webhook không hợp lệ");
            }

            long orderCode = webhookData.orderCode;

            // Get booking details with related data
            var booking = await context
                .Bookings.Include(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.PayOSOrderCode == orderCode, cancellationToken);

            if (booking == null)
            {
                logger.LogError("Booking not found: {orderCode}", orderCode);
                return Result.NotFound("Không tìm thấy booking");
            }

            bool isExtensionPayment = webhookData.description == "Thanh toan gia han";

            var transactionType = isExtensionPayment
                ? TransactionTypeNames.ExtensionPayment
                : TransactionTypeNames.BookingPayment;

            var paymentExists = await context.Transactions.AnyAsync(
                t => t.BookingId == booking.Id && t.Type.Name == transactionType,
                cancellationToken
            );

            if (paymentExists)
            {
                logger.LogWarning(
                    "Duplicate payment detected for BookingId: {BookingId}",
                    booking.Id
                );
                return Result.Conflict("Giao dịch đã được xử lý trước đó");
            }

            var transactionTypes = await context
                .TransactionTypes.Where(t =>
                    new[]
                    {
                        TransactionTypeNames.BookingPayment,
                        TransactionTypeNames.ExtensionPayment,
                        TransactionTypeNames.PlatformFee,
                        TransactionTypeNames.OwnerEarning
                    }.Contains(t.Name)
                )
                .ToListAsync(cancellationToken);

            if (transactionTypes.Count != 4)
                return Result.Error("Loại giao dịch không hợp lệ");

            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
            {
                logger.LogError("Admin user not found");
                return Result.Error("Không tìm thấy tài khoản admin");
            }

            GeneratePaymentTransactions(
                booking,
                transactionTypes,
                TransactionStatusEnum.Completed,
                admin,
                isExtensionPayment,
                out Transaction bookingPayment,
                out Transaction platformFeeTransaction,
                out Transaction ownerEarningTransaction
            );

            admin.Balance += booking.PlatformFee;
            booking.Car.Owner.LockedBalance += ownerEarningTransaction.Amount;

            var bookingLockedBalance = new BookingLockedBalance
            {
                OwnerId = booking.Car.Owner.Id,
                BookingId = booking.Id,
                Amount = ownerEarningTransaction.Amount
            };

            if (isExtensionPayment)
            {
                // Handle extension payment logic
                booking.ExtensionAmount = null; // Reset if necessary
                booking.IsExtensionPaid = true;
            }
            else
            {
                // Handle regular booking payment logic
                booking.IsPaid = true;
            }

            var cacheKey = $"PaymentLink_{booking.Id}";
            cache.Remove(cacheKey);

            context.Transactions.AddRange(
                bookingPayment,
                platformFeeTransaction,
                ownerEarningTransaction
            );
            context.BookingLockedBalances.Add(bookingLockedBalance);
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
            TransactionStatusEnum transactionStatus,
            User admin,
            bool isExtensionPayment,
            out Transaction bookingPayment,
            out Transaction platformFeeTransaction,
            out Transaction ownerEarningTransaction
        )
        {
            // Create transaction for booking payment or extension payment.
            bookingPayment = new Transaction
            {
                FromUserId = booking.UserId,
                ToUserId = admin.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionTypes
                    .First(t =>
                        t.Name
                        == (
                            isExtensionPayment
                                ? TransactionTypeNames.ExtensionPayment
                                : TransactionTypeNames.BookingPayment
                        )
                    )
                    .Id,
                Status = transactionStatus,
                Amount = isExtensionPayment ? booking.ExtensionAmount!.Value : booking.TotalAmount
            };

            // Create transaction for platform (admin) earning.
            platformFeeTransaction = new Transaction
            {
                FromUserId = admin.Id, // funds originate from admin's wallet
                ToUserId = admin.Id,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = transactionTypes.First(t => t.Name == TransactionTypeNames.PlatformFee).Id,
                Status = transactionStatus,
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
                Status = transactionStatus,
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
