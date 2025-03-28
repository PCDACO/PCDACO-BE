using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class CancelBooking
{
    public sealed record Command(Guid BookingId, string CancelReason = "") : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        private const decimal ADMIN_REFUND_PERCENTAGE = 0.1m;
        private const decimal OWNER_REFUND_PERCENTAGE = 0.9m;
        private const decimal REFUND_PERCENTAGE_BEFORE_7_DAYS = 1.0m;
        private const decimal REFUND_PERCENTAGE_BEFORE_5_DAYS = 0.5m;
        private const decimal REFUND_PERCENTAGE_BEFORE_3_DAYS = 0.3m;
        private const decimal REFUND_PERCENTAGE_BEFORE_1_DAY = 0m;

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Add cancellation limit check
            var recentCancellations = await context.Bookings.CountAsync(
                b =>
                    b.UserId == currentUser.User.Id
                    && b.Status == BookingStatusEnum.Cancelled
                    && b.UpdatedAt >= DateTime.UtcNow.AddDays(-30),
                cancellationToken
            );

            if (recentCancellations >= 5)
                return Result.Error("Bạn đã hủy quá số lần cho phép trong 30 ngày");

            if (
                booking.Status
                is (
                    BookingStatusEnum.Rejected
                    or BookingStatusEnum.Ongoing
                    or BookingStatusEnum.Completed
                    or BookingStatusEnum.Cancelled
                    or BookingStatusEnum.Expired
                )
            )
            {
                return Result.Conflict(
                    $"Không thể hủy booking ở trạng thái " + booking.Status.ToString()
                );
            }

            // Calculate days until start time
            var daysUntilStart = (booking.StartTime - DateTimeOffset.UtcNow).TotalDays;

            // Calculate refund percentage based on cancellation timing
            decimal refundPercentage = CalculateRefundPercentage(daysUntilStart);

            if (booking.Status == BookingStatusEnum.Approved)
                booking.Car.Status = CarStatusEnum.Available;

            if (booking.IsPaid)
            {
                await HandleRefundTransactions(
                    booking,
                    refundPercentage,
                    $"Hủy đặt xe {request.CancelReason}",
                    cancellationToken
                );
            }

            booking.Status = BookingStatusEnum.Cancelled;
            booking.Note = request.CancelReason;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // TODO: send email to both Owner and Driver with refund details

            return Result.SuccessWithMessage(
                $"Đã hủy booking thành công. {(booking.IsPaid ? $"Số tiền hoàn trả: {booking.RefundAmount:N0} VND ({refundPercentage * 100}%)" : "")}"
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

        private async Task HandleRefundTransactions(
            Booking booking,
            decimal refundPercentage,
            string reason,
            CancellationToken cancellationToken
        )
        {
            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
                throw new InvalidOperationException("Admin user not found");

            var refundAmount = booking.TotalAmount * refundPercentage;
            var adminRefundAmount = refundAmount * ADMIN_REFUND_PERCENTAGE;
            var ownerRefundAmount = refundAmount * OWNER_REFUND_PERCENTAGE;

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
                BalanceAfter = booking.User.Balance + refundAmount
            };

            var ownerAvailableBalance = booking.Car.Owner.Balance - booking.Car.Owner.LockedBalance;
            if (ownerAvailableBalance < ownerRefundAmount)
            {
                // If owner has insufficient available balance, take from locked balance
                booking.Car.Owner.LockedBalance -= ownerRefundAmount;
            }

            booking.Car.Owner.Balance -= ownerRefundAmount;
            admin.Balance -= adminRefundAmount;
            booking.User.Balance += refundAmount;

            // Update booking refund info
            booking.IsRefund = true;
            booking.RefundAmount = refundAmount;
            booking.RefundDate = DateTimeOffset.UtcNow;

            context.Transactions.AddRange(adminRefundTransaction, ownerRefundTransaction);
        }
    }
}
