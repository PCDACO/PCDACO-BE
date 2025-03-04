using Ardalis.Result;
using Domain.Constants.EntityNames;
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
                .ThenInclude(c => c.CarStatus)
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

            var carStatus = await context
                .CarStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, CarStatusNames.Available),
                    cancellationToken
                );

            if (carStatus == null)
                return Result.NotFound("Không tìm thấy trạng thái xe phù hợp");

            if (booking.Status.Name == BookingStatusEnum.Approved.ToString())
                booking.Car.StatusId = carStatus.Id;

            booking.StatusId = status.Id;
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Đã hủy booking thành công");
        }
    }
}
