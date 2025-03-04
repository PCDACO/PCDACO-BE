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
                .Bookings.Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
            {
                return Result.NotFound("Không tìm thấy booking");
            }

            // Ensure the booking is in a valid state (i.e. Approved) before
            // transitioning to ReadyForPickup.
            if (booking.Status.Name.ToEnum() != BookingStatusEnum.Approved)
            {
                return Result.Conflict(
                    $"Không thể chuyển booking sang Ready For Pickup từ trạng thái {booking.Status.Name}"
                );
            }

            var readyStatus = await context.BookingStatuses.FirstOrDefaultAsync(
                x => EF.Functions.Like(x.Name, BookingStatusEnum.ReadyForPickup.ToString()),
                cancellationToken
            );

            if (readyStatus == null)
            {
                return Result.NotFound("Không tìm thấy trạng thái 'Ready For Pickup'");
            }

            booking.StatusId = readyStatus.Id;
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(
                "Booking đã được chuyển sang trạng thái Ready For Pickup"
            );
        }
    }
}
