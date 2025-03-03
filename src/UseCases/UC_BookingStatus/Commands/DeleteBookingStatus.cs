using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BookingStatus.Commands;

public class DeleteBookingStatus
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if the user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này !");
            // Check if the booking status is existed
            BookingStatus? updatingBookingStatus = await context.BookingStatuses
                .Include(bs => bs.Bookings)
                .FirstOrDefaultAsync(bs => bs.Id == request.Id && !bs.IsDeleted, cancellationToken);
            if (updatingBookingStatus is null)
                return Result.NotFound("Trạng thái đặt phòng không tồn tại !");
            if (updatingBookingStatus.Bookings.Count != 0)
                return Result.Error("Không thể xóa trạng thái đặt phòng đã được sử dụng !");
            // Delete booking status
            updatingBookingStatus.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa trạng thái đặt phòng thành công !");
        }
    }
}