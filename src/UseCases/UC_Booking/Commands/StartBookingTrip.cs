using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class StartBookingTrip
{
    public sealed record Command(Guid BookingId, decimal Latitude, decimal Longitude)
        : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context.Bookings.FirstOrDefaultAsync(
                x => x.Id == request.BookingId,
                cancellationToken
            );

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

            // Create current location point
            var currentLocation = geometryFactory.CreatePoint(
                new Coordinate((double)request.Longitude, (double)request.Latitude)
            );
            currentLocation.SRID = 4326; // Set SRID for GPS coordinates

            var tracking = new TripTracking
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                BookingId = booking.Id,
                Location = currentLocation,
                Distance = 0,
                CumulativeDistance = 0,
            };

            booking.Status = BookingStatusEnum.Ongoing;
            booking.IsCarReturned = false;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            await context.TripTrackings.AddAsync(tracking, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Đã bắt đầu chuyến đi");
        }
    }
}
