using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public sealed class CancelBooking
{
    public sealed record Command(Guid BookingId, string CancelReason = "") : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        private const decimal REFUND_PERCENTAGE_BEFORE_7_DAYS = 1.0m;
        private const decimal REFUND_PERCENTAGE_BEFORE_5_DAYS = 0.5m;
        private const decimal REFUND_PERCENTAGE_BEFORE_3_DAYS = 0.3m;
        private const decimal REFUND_PERCENTAGE_BEFORE_1_DAY = 0m;
        private const decimal OWNER_PENALTY_WITHIN_24H = 0.5m;
        private const decimal OWNER_PENALTY_WITHIN_3_DAYS = 0.3m;
        private const decimal OWNER_PENALTY_WITHIN_7_DAYS = 0.1m;

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var booking = await context
                .Bookings.Include(x => x.Car)
                .ThenInclude(x => x.Owner)
                .ThenInclude(o => o.BookingLockedBalances)
                .Include(x => x.User)
                .Include(x => x.Car.Model)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
            {
                logger.LogWarning("Booking with ID {BookingId} not found", request.BookingId);
                return Result.NotFound("Không tìm thấy booking");
            }

            // Check if user is either the driver or the owner
            bool isDriver = booking.UserId == currentUser.User!.Id;
            bool isOwner = booking.Car.OwnerId == currentUser.User!.Id;

            if (!isDriver && !isOwner)
            {
                logger.LogWarning(
                    "User {UserId} attempted to cancel booking {BookingId} without permission",
                    currentUser.User.Id,
                    request.BookingId
                );
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );
            }

            if (
                booking.Status
                is (
                    BookingStatusEnum.Rejected
                    or BookingStatusEnum.Ongoing
                    or BookingStatusEnum.Completed
                    or BookingStatusEnum.Done
                    or BookingStatusEnum.Cancelled
                    or BookingStatusEnum.Expired
                )
            )
            {
                logger.LogWarning(
                    "User {UserId} attempted to cancel booking {BookingId} in status {Status}",
                    currentUser.User.Id,
                    request.BookingId,
                    booking.Status
                );
                return Result.Conflict($"Không thể hủy booking ở trạng thái {booking.Status}");
            }

            // Calculate days until start time
            var daysUntilStart = (booking.StartTime - DateTimeOffset.UtcNow).TotalDays;
            decimal penaltyAmount = 0;

            if (isDriver)
            {
                // Add cancellation limit check
                var recentCancellations = await context.Bookings.CountAsync(
                    b =>
                        b.UserId == currentUser.User.Id
                        && b.Status == BookingStatusEnum.Cancelled
                        && b.UpdatedAt >= DateTime.UtcNow.AddDays(-30),
                    cancellationToken
                );

                if (recentCancellations >= 5)
                {
                    logger.LogWarning(
                        "User {UserId} exceeded cancellation limit for booking {BookingId}",
                        currentUser.User.Id,
                        request.BookingId
                    );
                    return Result.Error("Bạn đã hủy quá số lần cho phép trong 30 ngày");
                }

                // Calculate refund percentage based on cancellation timing
                decimal refundPercentage = CalculateRefundPercentage(daysUntilStart);

                if (booking.Status == BookingStatusEnum.Approved)
                    booking.Car.Status = CarStatusEnum.Available;

                if (booking.IsPaid)
                {
                    await HandleDriverCancellationRefund(
                        booking,
                        refundPercentage,
                        $"Hủy đặt xe {request.CancelReason}",
                        cancellationToken
                    );
                }
            }
            else // Owner cancellation
            {
                // Calculate penalty for owner
                decimal penaltyPercentage = CalculateOwnerPenalty(daysUntilStart);
                penaltyAmount = booking.TotalAmount * penaltyPercentage;

                if (booking.IsPaid)
                {
                    // Refund full amount to driver
                    await HandleOwnerCancellationRefund(
                        booking,
                        penaltyAmount,
                        $"Chủ xe hủy đặt xe: {request.CancelReason}",
                        cancellationToken
                    );
                }

                // Apply penalty to owner's account
                booking.Car.Owner.Balance -= penaltyAmount;

                // Update car status
                if (booking.Status == BookingStatusEnum.Approved)
                {
                    logger.LogInformation(
                        "Car status updated to available for booking {BookingId}",
                        request.BookingId
                    );
                    booking.Car.Status = CarStatusEnum.Available;
                }
            }

            booking.Status = BookingStatusEnum.Cancelled;
            booking.Note = request.CancelReason;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Booking {BookingId} cancelled successfully by user {UserId}",
                request.BookingId,
                currentUser.User.Id
            );

            // Send cancellation emails
            await SendCancellationEmails(
                driverName: booking.User.Name,
                driverEmail: booking.User.Email,
                ownerName: booking.Car.Owner.Name,
                ownerEmail: booking.Car.Owner.Email,
                carName: booking.Car.Model.Name,
                startTime: booking.StartTime,
                endTime: booking.EndTime,
                amount: isOwner ? penaltyAmount : (booking.RefundAmount ?? 0),
                cancelReason: request.CancelReason,
                isOwnerCancelled: isOwner
            );
            logger.LogInformation(
                "Cancellation emails sent for booking {BookingId}",
                request.BookingId
            );

            return Result.SuccessWithMessage(
                isOwner
                    ? $"Đã hủy booking thành công. Tiền phạt: {CalculateOwnerPenalty(daysUntilStart) * 100}% ({booking.TotalAmount * CalculateOwnerPenalty(daysUntilStart):N0} VND)"
                    : $"Đã hủy booking thành công. {(booking.IsPaid ? $"Số tiền hoàn trả: {booking.RefundAmount:N0} VND" : "")}"
            );
        }

        private static decimal CalculateRefundPercentage(double daysUntilStart) =>
            daysUntilStart switch
            {
                >= 7 => REFUND_PERCENTAGE_BEFORE_7_DAYS,
                >= 5 => REFUND_PERCENTAGE_BEFORE_5_DAYS,
                >= 3 => REFUND_PERCENTAGE_BEFORE_3_DAYS,
                _ => REFUND_PERCENTAGE_BEFORE_1_DAY,
            };

        private async Task HandleDriverCancellationRefund(
            Booking booking,
            decimal refundPercentage,
            string reason,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation(
                "Processing driver cancellation refund for booking {BookingId}",
                booking.Id
            );

            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
            {
                logger.LogError("Admin user not found for booking {BookingId}", booking.Id);
                throw new InvalidOperationException("Admin user not found");
            }

            var adminRefundAmount = booking.PlatformFee * refundPercentage;
            var ownerRefundAmount = booking.BasePrice * refundPercentage;
            var refundAmount = adminRefundAmount + ownerRefundAmount;

            var transactionTypes = await context
                .TransactionTypes.Where(t => new[] { TransactionTypeNames.Refund }.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var refundType = transactionTypes.First(t => t.Name == TransactionTypeNames.Refund);

            // Create refund transactions
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
                BalanceAfter = booking.User.Balance + ownerRefundAmount
            };

            // Get the booking's locked balance
            var bookingLockedBalance = booking.Car.Owner.BookingLockedBalances.FirstOrDefault(b =>
                b.BookingId == booking.Id
            );

            if (bookingLockedBalance != null)
            {
                if (bookingLockedBalance.Amount >= ownerRefundAmount)
                {
                    // Use booking's locked balance for refund
                    bookingLockedBalance.Amount -= ownerRefundAmount;
                    booking.Car.Owner.LockedBalance -= ownerRefundAmount;

                    logger.LogInformation(
                        "Refunding {Amount} from booking's locked balance for booking {BookingId}",
                        ownerRefundAmount,
                        booking.Id
                    );

                    // If there's remaining locked balance, move it to available balance
                    if (bookingLockedBalance.Amount > 0)
                    {
                        booking.Car.Owner.Balance += bookingLockedBalance.Amount;
                        booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                        bookingLockedBalance.Amount = 0;

                        logger.LogInformation(
                            "Remaining locked balance moved to available balance for booking {BookingId}",
                            booking.Id
                        );
                    }
                }
                else
                {
                    // If booking's locked balance is insufficient, use all of it and take the rest from available balance
                    var remainingRefund = ownerRefundAmount - bookingLockedBalance.Amount;
                    booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                    booking.Car.Owner.Balance -= remainingRefund;
                    bookingLockedBalance.Amount = 0;

                    logger.LogInformation(
                        "Refunding {Amount} from booking's locked balance and {RemainingRefund} from available balance for booking {BookingId}",
                        bookingLockedBalance.Amount,
                        remainingRefund,
                        booking.Id
                    );
                }
            }
            else
            {
                // If no locked balance record found, take from available balance
                booking.Car.Owner.Balance -= ownerRefundAmount;

                logger.LogInformation(
                    "Refunding {Amount} from available balance for booking {BookingId}",
                    ownerRefundAmount,
                    booking.Id
                );
            }

            admin.Balance -= adminRefundAmount;
            booking.User.Balance += refundAmount;

            // Update booking refund info
            booking.IsRefund = true;
            booking.RefundAmount = refundAmount;
            booking.RefundDate = DateTimeOffset.UtcNow;

            context.Transactions.AddRange(adminRefundTransaction, ownerRefundTransaction);
        }

        private static decimal CalculateOwnerPenalty(double daysUntilStart) =>
            daysUntilStart switch
            {
                < 1 => OWNER_PENALTY_WITHIN_24H,
                < 3 => OWNER_PENALTY_WITHIN_3_DAYS,
                < 7 => OWNER_PENALTY_WITHIN_7_DAYS,
                _ => 0
            };

        private async Task HandleOwnerCancellationRefund(
            Booking booking,
            decimal penaltyAmount,
            string reason,
            CancellationToken cancellationToken
        )
        {
            logger.LogInformation(
                "Processing owner cancellation refund for booking {BookingId}",
                booking.Id
            );

            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
                throw new InvalidOperationException("Admin user not found");

            var transactionTypes = await context
                .TransactionTypes.Where(t => new[] { TransactionTypeNames.Refund }.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var refundType = transactionTypes.First(t => t.Name == TransactionTypeNames.Refund);

            // Create a full refund transaction for the driver
            var refundTransaction = new Transaction
            {
                FromUserId = booking.Car.OwnerId,
                ToUserId = booking.UserId,
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = refundType.Id,
                Status = TransactionStatusEnum.Completed,
                Amount = booking.TotalAmount,
                Description = reason,
                BalanceAfter = booking.User.Balance + booking.TotalAmount
            };

            // Create penalty transaction
            var penaltyTransaction = new Transaction
            {
                FromUserId = booking.Car.OwnerId,
                ToUserId = admin.Id, // Penalty goes to admin
                BookingId = booking.Id,
                BankAccountId = null,
                TypeId = refundType.Id,
                Status = TransactionStatusEnum.Completed,
                Amount = penaltyAmount,
                Description = $"Phí phạt hủy booking: {reason}",
                BalanceAfter = admin.Balance + penaltyAmount
            };

            // Get the booking's locked balance
            var bookingLockedBalance = booking.Car.Owner.BookingLockedBalances.FirstOrDefault(b =>
                b.BookingId == booking.Id
            );

            if (bookingLockedBalance != null)
            {
                if (bookingLockedBalance.Amount >= booking.TotalAmount)
                {
                    // Use booking's locked balance for refund
                    bookingLockedBalance.Amount -= booking.TotalAmount;
                    booking.Car.Owner.LockedBalance -= booking.TotalAmount;

                    logger.LogInformation(
                        "Refunding {Amount} from booking's locked balance for booking {BookingId}",
                        booking.TotalAmount,
                        booking.Id
                    );

                    // If there's remaining locked balance, move it to available balance
                    if (bookingLockedBalance.Amount > 0)
                    {
                        booking.Car.Owner.Balance += bookingLockedBalance.Amount;
                        booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                        bookingLockedBalance.Amount = 0;

                        logger.LogInformation(
                            "Remaining locked balance moved to available balance for booking {BookingId}",
                            booking.Id
                        );
                    }
                }
                else
                {
                    // If booking's locked balance is insufficient, use all of it and take the rest from available balance
                    var remainingRefund = booking.TotalAmount - bookingLockedBalance.Amount;
                    booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                    booking.Car.Owner.Balance -= remainingRefund;
                    bookingLockedBalance.Amount = 0;

                    logger.LogInformation(
                        "Refunding {Amount} from booking's locked balance and {RemainingRefund} from available balance for booking {BookingId}",
                        bookingLockedBalance.Amount,
                        remainingRefund,
                        booking.Id
                    );
                }
            }
            else
            {
                // If no locked balance record found, take from available balance
                booking.Car.Owner.Balance -= booking.TotalAmount;

                logger.LogInformation(
                    "Refunding {Amount} from available balance for booking {BookingId}",
                    booking.TotalAmount,
                    booking.Id
                );
            }

            // Apply penalty from available balance
            booking.Car.Owner.Balance -= penaltyAmount;
            admin.Balance += penaltyAmount;
            booking.User.Balance += booking.TotalAmount;

            context.Transactions.AddRange(refundTransaction, penaltyTransaction);
        }

        private async Task SendCancellationEmails(
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carName,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            decimal amount,
            string cancelReason,
            bool isOwnerCancelled
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverCancelBookingTemplate.Template(
                driverName,
                carName,
                startTime,
                endTime,
                amount,
                cancelReason,
                isOwnerCancelled
            );
            await emailService.SendEmailAsync(
                driverEmail,
                isOwnerCancelled ? "Chủ Xe Đã Hủy Đơn" : "Xác Nhận Hủy Đơn",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerCancelBookingTemplate.Template(
                ownerName,
                carName,
                driverName,
                startTime,
                endTime,
                amount,
                cancelReason,
                isOwnerCancelled
            );
            await emailService.SendEmailAsync(
                ownerEmail,
                isOwnerCancelled ? "Xác Nhận Hủy Đơn" : "Khách Hàng Đã Hủy Đơn",
                ownerEmailTemplate
            );
        }
    }
}
