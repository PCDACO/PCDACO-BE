using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class ApproveBooking
{
    public sealed record Command(Guid BookingId, bool IsApproved) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Status)
                .Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            // Validate current status
            var invalidStatuses = new[]
            {
                BookingStatusEnum.Approved,
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

            string statusName = request.IsApproved
                ? BookingStatusEnum.Approved.ToString()
                : BookingStatusEnum.Rejected.ToString();
            string message = request.IsApproved ? "phê duyệt" : "từ chối";

            var status = await context.BookingStatuses.FirstOrDefaultAsync(
                x => EF.Functions.Like(x.Name, statusName),
                cancellationToken
            );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            booking.StatusId = status.Id;
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage($"Đã {message} booking thành công");
        }
    }
}
