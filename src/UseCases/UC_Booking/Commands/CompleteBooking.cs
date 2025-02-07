using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class CompleteBooking
{
    public sealed record Command(Guid BookingId) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Status)
                .Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            var invalidStatuses = new[]
            {
                BookingStatusEnum.Pending,
                BookingStatusEnum.Approved,
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled
            };

            if (invalidStatuses.Contains(booking.Status.Name.ToEnum()))
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái {booking.Status.Name}"
                );
            }

            var status = await context.BookingStatuses.FirstOrDefaultAsync(
                x => EF.Functions.Like(x.Name, BookingStatusEnum.Completed.ToString()),
                cancellationToken
            );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            var lastTracking = await context
                .TripTrackings.Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            decimal totalDistance = lastTracking?.CumulativeDistance ?? 0;

            // Set actual return time and calculate excess fees
            booking.ActualReturnTime = DateTimeOffset.UtcNow;
            var (excessDays, excessFee) = CalculateExcessFee(booking);

            booking.StatusId = status.Id;
            booking.ExcessDay = excessDays;
            booking.ExcessDayFee = excessFee;
            booking.TotalAmount = booking.BasePrice + booking.PlatformFee + excessFee;

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(
                $"""
                Đã hoàn thành chuyến đi
                Tổng quãng đường: {totalDistance / 1000:N2} km
                Số ngày trễ: {excessDays}
                Phí phát sinh: {excessFee:N0} VND
                Tổng cộng: {booking.TotalAmount:N0} VND
                """
            );
        }

        private static (decimal ExcessDays, decimal ExcessFee) CalculateExcessFee(Booking booking)
        {
            // If returned before or on time
            if (booking.ActualReturnTime <= booking.EndTime)
            {
                return (0, 0);
            }

            // Calculate excess days (round up to full days)
            var excessTimeSpan = booking.ActualReturnTime - booking.EndTime;
            var excessDays = Math.Ceiling(excessTimeSpan.TotalDays);

            // Calculate daily rate from the original booking
            var plannedDays = (booking.EndTime - booking.StartTime).TotalDays;
            var dailyRate = booking.BasePrice / (decimal)plannedDays;

            // Apply penalty multiplier (e.g., 1.5x the daily rate for excess days)
            const decimal penaltyMultiplier = 1.5m;
            var excessFee = dailyRate * (decimal)excessDays * penaltyMultiplier;

            return ((decimal)excessDays, excessFee);
        }
    }
}
