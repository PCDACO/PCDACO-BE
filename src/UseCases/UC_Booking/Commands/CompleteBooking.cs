using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public sealed class CompleteBooking
{
    public sealed record Command(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(
        decimal TotalDistance,
        decimal UnusedDays,
        decimal RefundAmount,
        decimal ExcessDays,
        decimal ExcessFee,
        decimal BasePrice,
        decimal PlatformFee,
        decimal FinalAmount
    );

    internal sealed class Handler(
        IAppDBContext context,
        IBackgroundJobClient backgroundJobClient,
        IEmailService emailService,
        ILogger<Handler> logger,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        private const decimal MAX_ALLOWED_DISTANCE_METERS = 5000;
        private const decimal EARLY_RETURN_REFUND_PERCENTAGE = 0.5m; // 50% refund for unused days if less than half of total days
        private const decimal LATE_RETURN_PENALTY_MULTIPLIER = 1.2m; // 120% of daily rate for late days
        private const int GRACE_PERIOD_HOURS = 3; // Grace period of 3 hours
        private const double LATE_DAY_THRESHOLD = 0.25; // 6 hours (0.25 days) threshold for counting as a new day

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .ThenInclude(x => x.Model)
                .Include(x => x.Car)
                .ThenInclude(x => x.Owner)
                .Include(x => x.Car)
                .ThenInclude(x => x.GPS)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            if (booking.Status != BookingStatusEnum.Ongoing)
            {
                logger.LogWarning(
                    "User {UserId} tried to complete booking {BookingId} in status {Status}",
                    currentUser.User.Id,
                    booking.Id,
                    booking.Status
                );
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }

            if (booking.Car.GPS == null || booking.Car.GPS.Location == null)
                return Result.Error("Không thể xác định vị trí hiện tại của xe");

            // Calculate distance from car's pickup location
            var distanceInMeters =
                (decimal)booking.Car.PickupLocation.Distance(booking.Car.GPS.Location) * 111320m;

            if (distanceInMeters > MAX_ALLOWED_DISTANCE_METERS)
            {
                logger.LogWarning(
                    "User {UserId} tried to complete booking {BookingId} outside allowed distance",
                    currentUser.User.Id,
                    booking.Id
                );
                return Result.Error(
                    $"Xe phải được trả tại địa điểm đã đón: {booking.Car.PickupAddress}. "
                        + $"Vui lòng di chuyển đến trong phạm vi {MAX_ALLOWED_DISTANCE_METERS} mét so với vị trí đón xe!"
                );
            }

            var lastTracking = await context
                .TripTrackings.Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            decimal totalDistance = lastTracking?.CumulativeDistance ?? 0;

            // Calculate actual rental duration
            var actualReturnTime = DateTimeOffset.UtcNow;
            var totalBookingDays = Math.Ceiling(
                (decimal)(booking.EndTime - booking.StartTime).TotalDays
            );

            // Calculate the time difference in hours
            var overtimeHours = (actualReturnTime - booking.EndTime).TotalHours;
            var actualDays = (decimal)(actualReturnTime - booking.StartTime).TotalDays;

            decimal refundAmount = 0;
            decimal excessDays = 0;
            decimal excessFee = 0;
            decimal unusedDays = 0;

            var dailyRate = booking.BasePrice / totalBookingDays;

            var refundType = await context.TransactionTypes.FirstAsync(
                t => t.Name == TransactionTypeNames.Refund,
                cancellationToken
            );

            var admin = await context.Users.FirstOrDefaultAsync(
                u => u.Role.Name == UserRoleNames.Admin,
                cancellationToken
            );

            if (admin == null)
                return Result.Error("Không tìm thấy admin");

            // Early Return Case with 50% refund
            if (actualDays < (totalBookingDays / 2) && actualDays >= 1)
            {
                unusedDays = totalBookingDays - actualDays;
                // Calculate refund as 50% of total booking amount
                refundAmount = booking.TotalAmount * EARLY_RETURN_REFUND_PERCENTAGE;

                logger.LogInformation(
                    "User {UserId} returned booking {BookingId} early, refund amount: {RefundAmount}",
                    currentUser.User.Id,
                    booking.Id,
                    refundAmount
                );

                // Calculate refund portions
                var adminRefundAmount = refundAmount * 0.1m; // 10% from admin
                var ownerRefundAmount = refundAmount * 0.9m; // 90% from owner

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
                    Description = "Hoàn tiền từ Admin: Trả xe sớm",
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
                    Description = "Hoàn tiền từ Chủ xe: Trả xe sớm",
                    BalanceAfter = booking.User.Balance + refundAmount
                };

                // Get the booking's locked balance
                var bookingLockedBalance = booking.Car.Owner.BookingLockedBalances.FirstOrDefault(
                    b => b.BookingId == booking.Id
                );

                if (bookingLockedBalance != null)
                {
                    if (bookingLockedBalance.Amount >= ownerRefundAmount)
                    {
                        // Use booking's locked balance for refund
                        bookingLockedBalance.Amount -= ownerRefundAmount;
                        booking.Car.Owner.LockedBalance -= ownerRefundAmount;

                        // If there's remaining locked balance, move it to available balance
                        if (bookingLockedBalance.Amount > 0)
                        {
                            booking.Car.Owner.Balance += bookingLockedBalance.Amount;
                            booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                            bookingLockedBalance.Amount = 0;
                        }
                    }
                    else
                    {
                        // If booking's locked balance is insufficient, use all of it and take the rest from available balance
                        var remainingRefund = ownerRefundAmount - bookingLockedBalance.Amount;
                        booking.Car.Owner.LockedBalance -= bookingLockedBalance.Amount;
                        booking.Car.Owner.Balance -= remainingRefund;
                        bookingLockedBalance.Amount = 0;
                    }
                }
                else
                {
                    // If no locked balance record found, take from available balance
                    booking.Car.Owner.Balance -= ownerRefundAmount;
                }

                admin.Balance -= adminRefundAmount;
                booking.User.Balance += refundAmount;

                context.Transactions.AddRange(adminRefundTransaction, ownerRefundTransaction);
            }
            // Late Return Case
            else if (overtimeHours > GRACE_PERIOD_HOURS)
            {
                var overtimeDays = overtimeHours / 24.0;

                if (overtimeDays > LATE_DAY_THRESHOLD)
                {
                    excessDays = Math.Ceiling((decimal)overtimeDays);
                    excessFee = dailyRate * excessDays * LATE_RETURN_PENALTY_MULTIPLIER;
                }

                logger.LogInformation(
                    "User {UserId} returned booking {BookingId} late, excess days: {ExcessDays}, excess fee: {ExcessFee}",
                    currentUser.User.Id,
                    booking.Id,
                    excessDays,
                    excessFee
                );
            }

            // Calculate final amount
            var finalAmount = booking.BasePrice + booking.PlatformFee + excessFee - refundAmount;

            // Update booking
            booking.Status = BookingStatusEnum.Completed;
            booking.TotalDistance = totalDistance;
            booking.ActualReturnTime = actualReturnTime;
            booking.ExcessDay = excessDays;
            booking.ExcessDayFee = excessFee;
            booking.TotalAmount = finalAmount;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            if (refundAmount > 0)
            {
                logger.LogInformation(
                    "User {UserId} received refund for booking {BookingId}, amount: {RefundAmount}",
                    currentUser.User.Id,
                    booking.Id,
                    refundAmount
                );

                booking.IsRefund = true;
                booking.RefundAmount = refundAmount;
                booking.RefundDate = DateTimeOffset.UtcNow;
            }

            // After handling all cases, if there's any remaining locked balance for this booking, move it to available balance
            var remainingBookingLockedBalance =
                booking.Car.Owner.BookingLockedBalances.FirstOrDefault(b =>
                    b.BookingId == booking.Id
                );

            if (remainingBookingLockedBalance != null && remainingBookingLockedBalance.Amount > 0)
            {
                booking.Car.Owner.Balance += remainingBookingLockedBalance.Amount;
                booking.Car.Owner.LockedBalance -= remainingBookingLockedBalance.Amount;
                remainingBookingLockedBalance.Amount = 0;
            }

            await context.SaveChangesAsync(cancellationToken);

            // Send notification emails
            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        booking.User.Name,
                        booking.Car.Owner.Name,
                        booking.User.Email,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        totalDistance,
                        booking.BasePrice,
                        excessFee,
                        booking.PlatformFee,
                        finalAmount
                    )
            );

            return Result.Success(
                new Response(
                    TotalDistance: totalDistance / 1000, // Convert to kilometers
                    UnusedDays: unusedDays,
                    RefundAmount: refundAmount,
                    ExcessDays: excessDays,
                    ExcessFee: excessFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    FinalAmount: finalAmount
                )
            );
        }

        public async Task SendEmail(
            string driverName,
            string ownerName,
            string driverEmail,
            string ownerEmail,
            string carModelName,
            decimal totalDistance,
            decimal basePrice,
            decimal excessDayFee,
            decimal platformFee,
            decimal totalAmount
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverCompleteBookingTemplate.Template(
                driverName,
                carModelName,
                totalDistance / 1000, // Convert to kilometers
                basePrice,
                excessDayFee,
                platformFee,
                totalAmount
            );

            await emailService.SendEmailAsync(
                driverEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerBookingCompletedTemplate.Template(
                ownerName,
                driverName,
                carModelName,
                totalDistance / 1000, // Convert to kilometers
                basePrice,
                excessDayFee,
                platformFee,
                totalAmount,
                totalAmount - platformFee
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                ownerEmailTemplate
            );
        }
    }
}
