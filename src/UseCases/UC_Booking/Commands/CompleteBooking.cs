using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class CompleteBooking
{
    public sealed record Command(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(
        decimal TotalDistance,
        decimal BasePrice,
        decimal PlatformFee,
        decimal TotalAmount
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
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
            if (booking.Status != BookingStatusEnum.Ongoing)
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }
            var lastTracking = await context
                .TripTrackings.Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            decimal totalDistance = lastTracking?.CumulativeDistance ?? 0;

            booking.Status = BookingStatusEnum.Completed;
            booking.TotalDistance = totalDistance;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new Response(
                    TotalDistance: totalDistance / 1000, // Convert to kilometers
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    TotalAmount: booking.TotalAmount
                )
            );
        }
    }
}