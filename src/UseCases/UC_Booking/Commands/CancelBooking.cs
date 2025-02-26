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
                .ThenInclude(x => x.CarStatistic)
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
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Ongoing,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled,
                BookingStatusEnum.Expired
            };

            if (invalidStatuses.Contains(booking.Status.Name.ToEnum()))
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái {booking.Status.Name}"
                );
            }

            var status = await context
                .BookingStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, BookingStatusEnum.Cancelled.ToString()),
                    cancellationToken
                );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            var userStatistic = await context.UserStatistics.FirstOrDefaultAsync(
                x => x.UserId == currentUser.User.Id,
                cancellationToken
            );

            if (userStatistic == null)
                return Result.NotFound("Không tìm thấy thông tin thống kê của user");

            // Update car statistic
            booking.StatusId = status.Id;
            booking.Car.CarStatistic.TotalCancellation += 1;
            userStatistic.TotalCancel += 1;

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Đã hủy booking thành công");
        }
    }
}
