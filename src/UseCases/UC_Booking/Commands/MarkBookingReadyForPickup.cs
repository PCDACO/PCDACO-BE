using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class MarkBookingReadyForPickup
{
    public sealed record Command(Guid BookingId) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsOwner())
            {
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này!");
            }

            var booking = await context
                .Bookings.Include(x => x.Car)
                .Include(x => x.CarInspections)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
            {
                return Result.NotFound("Không tìm thấy booking");
            }

            if (booking.Car.OwnerId != currentUser.User.Id)
            {
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );
            }

            if (!booking.CarInspections.Any())
                return Result.Error("Chưa có hình ảnh kiểm tra cho booking này!");

            // Ensure the booking is in a valid state
            if (booking.Status != BookingStatusEnum.Approved)
            {
                return Result.Conflict(
                    $"Không thể chuyển booking sang Ready For Pickup từ trạng thái "
                        + booking.Status.ToString()
                );
            }

            // Check if it's not too early (no more than 24h before start time)
            var timeUntilStart = booking.StartTime - DateTimeOffset.UtcNow;
            if (timeUntilStart.TotalHours > 24)
            {
                return Result.Error(
                    "Chỉ có thể chuyển trạng thái trong vòng 24 giờ trước giờ bắt đầu"
                );
            }

            booking.Status = BookingStatusEnum.ReadyForPickup;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // TODO: Send notification to driver that car is ready for pickup

            return Result.SuccessWithMessage(
                "Booking đã được chuyển sang trạng thái Ready For Pickup"
            );
        }
    }
}
