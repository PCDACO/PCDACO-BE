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
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            // Validate current status
            var invalidStatuses = new[]
            {
                BookingStatusEnum.Pending,
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Ongoing,
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
                x => EF.Functions.Like(x.Name, BookingStatusEnum.Ongoing.ToString()),
                cancellationToken
            );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            booking.StatusId = status.Id;
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Đã bắt đầu chuyến đi");
        }
    }
}
