using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class StartBookingTrip
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
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            if (booking.Status != BookingStatusEnum.ReadyForPickup)
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }
            booking.Status = BookingStatusEnum.Ongoing;
            booking.IsCarReturned = false;
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Đã bắt đầu chuyến đi");
        }
    }
}