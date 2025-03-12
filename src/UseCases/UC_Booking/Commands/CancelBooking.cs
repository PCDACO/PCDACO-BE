using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class CancelBooking
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

            if (
                booking.Status == BookingStatusEnum.Rejected &&
                booking.Status == BookingStatusEnum.Ongoing &&
                booking.Status == BookingStatusEnum.Completed &&
                booking.Status == BookingStatusEnum.Cancelled &&
                booking.Status == BookingStatusEnum.Expired
            )
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }
            if (booking.Status == BookingStatusEnum.Approved)
                booking.Car.Status = CarStatusEnum.Available;
            decimal refundAmount = booking.CalculateRefundAmount();

            if (refundAmount > 0 && booking.IsPaid)
            {
                booking.IsRefund = true;
                booking.RefundAmount = refundAmount;
            }

            booking.Status = BookingStatusEnum.Cancelled;
            await context.SaveChangesAsync(cancellationToken);

            // TODO: send email to both Owner and Driver

            return Result.SuccessWithMessage("Đã hủy booking thành công");
        }
    }
}