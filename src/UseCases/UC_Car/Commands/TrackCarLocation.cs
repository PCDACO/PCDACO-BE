using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.Services.SignalR;
using UUIDNext;

namespace UseCases.UC_Car.Commands;

public sealed class TrackCarLocation
{
    public sealed record Command(Guid CarId, decimal Latitude, decimal Longitude)
        : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory,
        IHubContext<LocationHub> hubContext
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var car = await context
                .Cars.Include(c => c.GPS)
                .FirstOrDefaultAsync(x => x.Id == request.CarId, cancellationToken);

            if (car == null)
                return Result.NotFound("Không tìm thấy xe");

            if (car.GPS == null)
                return Result.Error("Xe chưa được cài đặt GPS");

            // Create current location point
            var currentLocation = geometryFactory.CreatePoint(
                new Coordinate((double)request.Longitude, (double)request.Latitude)
            );
            currentLocation.SRID = 4326;

            car.GPS.Location = currentLocation;
            car.GPS.UpdatedAt = DateTimeOffset.UtcNow;

            // Check for active booking and get latest tracking if exists
            var activeBooking = await context
                .Bookings.Where(b =>
                    b.CarId == request.CarId && b.Status == Domain.Enums.BookingStatusEnum.Ongoing
                )
                .Select(b => new
                {
                    b.Id,
                    LastTracking = b.TripTrackings.OrderByDescending(t => t.Id).FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (activeBooking != null)
            {
                decimal distance = 0;
                decimal cumulativeDistance = 0;

                if (activeBooking.LastTracking != null)
                {
                    const decimal metersPerDegree = 111320m;
                    distance =
                        (decimal)activeBooking.LastTracking.Location.Distance(currentLocation)
                        * metersPerDegree;
                    cumulativeDistance = activeBooking.LastTracking.CumulativeDistance + distance;
                }

                var tracking = new TripTracking
                {
                    Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                    BookingId = activeBooking.Id,
                    Location = currentLocation,
                    Distance = distance,
                    CumulativeDistance = cumulativeDistance
                };

                context.TripTrackings.Add(tracking);
            }

            // Send location update to SignalR clients
            await hubContext.Clients.All.SendAsync(
                "ReceiveLocationUpdate",
                car.Id,
                request.Latitude,
                request.Longitude,
                cancellationToken: cancellationToken
            );

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Cập nhật vị trí thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("Vĩ độ không hợp lệ");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Kinh độ không hợp lệ");
        }
    }
}
