using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class StartBookingTrip
{
    public sealed record Command(
        Guid BookingId,
        decimal Latitude,
        decimal Longitude,
        string Signature
    ) : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory,
        ILogger<Handler> logger,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        private const int MAX_ALLOWED_DISTANCE_METERS = 5000;
        private const decimal METERS_PER_DEGREE = 111320m; // 1 degree = 111320 meters

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(b => b.Car)
                .ThenInclude(c => c.GPS)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
            {
                logger.LogError("Booking not found: {BookingId}", request.BookingId);
                return Result.NotFound("Không tìm thấy booking");
            }

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            if (booking.Status != BookingStatusEnum.ReadyForPickup)
            {
                logger.LogError(
                    "Cannot start trip for booking {BookingId} in status {Status}",
                    booking.Id,
                    booking.Status
                );
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }

            var carGPS = booking.Car.GPS;

            if (carGPS == null)
            {
                logger.LogError("Car GPS not found for booking {BookingId}", booking.Id);
                return Result.NotFound("Không tìm thấy thiết bị GPS của xe");
            }

            if (!booking.IsPaid)
                return Result.Error("Cần thanh toán trước khi bắt đầu chuyến đi");

            // Create driver's location point
            var driverLocation = geometryFactory.CreatePoint(
                new Coordinate((double)request.Longitude, (double)request.Latitude)
            );
            driverLocation.SRID = 4326;

            // Calculate distance between driver and car
            var distanceInDegrees = carGPS.Location.Distance(driverLocation);
            var distanceInMeters = (decimal)distanceInDegrees * METERS_PER_DEGREE;

            if (distanceInMeters > MAX_ALLOWED_DISTANCE_METERS)
            {
                logger.LogWarning(
                    "Driver is too far from the car: {Distance} meters",
                    distanceInMeters
                );
                return Result.Error(
                    $"Bạn phải ở trong phạm vi {MAX_ALLOWED_DISTANCE_METERS}m từ xe để bắt đầu chuyến đi. "
                        + $"Hiện tại bạn cách xe {(int)distanceInMeters}m"
                );
            }

            // Check if booking contract exists
            var contract = await context.Contracts.FirstOrDefaultAsync(
                c => c.BookingId == booking.Id,
                cancellationToken
            );

            if (contract == null)
            {
                logger.LogError("Contract not found for booking {BookingId}", booking.Id);
                return Result.NotFound("Không tìm thấy hợp đồng cho booking này");
            }

            // Update driver signature
            contract.DriverSignature = request.Signature;
            contract.DriverSignatureDate = DateTimeOffset.UtcNow;

            var tracking = new TripTracking
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                BookingId = booking.Id,
                Location = driverLocation, // Use driver's location as starting point
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

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("ID booking không được để trống");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Cần đến gần chiếc xe thì mới bắt đầu được");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Cần đến gần chiếc xe thì mới bắt đầu được");

            RuleFor(x => x.Signature).NotEmpty().WithMessage("Chữ ký không được để trống");
        }
    }
}
