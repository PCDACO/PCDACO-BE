using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.SignalR;
using UUIDNext.Tools;

namespace UseCases.UC_Booking.Commands;

public sealed class BatchTrackTrip
{
    public sealed record Command(Guid BookingId, IEnumerable<LocationPoint> LocationPoints)
        : IRequest<Result>;

    public sealed record LocationPoint(
        decimal Latitude,
        decimal Longitude,
        DateTimeOffset CreatedAt
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        GeometryFactory geometryFactory,
        IHubContext<LocationHub> hubContext
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này!");

            var booking = await context
                .Bookings.Include(b => b.Status)
                .Include(b => b.TripTrackings)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            if (booking.Status.Name != BookingStatusEnum.Ongoing.ToString())
                return Result.Error("Chỉ có thể cập nhật vị trí khi chuyến đi đang diễn ra");

            if (!request.LocationPoints.Any())
                return Result.Error("Không có điểm định vị nào được gửi");

            var orderedPoints = request.LocationPoints.OrderBy(p => p.CreatedAt).ToList();

            var lastTracking = await context
                .TripTrackings.AsNoTracking()
                .Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var trackings = new List<TripTracking>();

            TripTracking? previousTracking = lastTracking;

            foreach (var point in orderedPoints)
            {
                Point currentLocation = CreateLocationPoint(point);

                // Calculate distance from last tracking point
                var (distance, cumulativeDistance) = CalculateDistance(
                    currentLocation,
                    previousTracking
                );

                var tracking = new TripTracking
                {
                    Id = UuidToolkit.CreateUuidV7FromSpecificDate(point.CreatedAt),
                    BookingId = booking.Id,
                    Location = currentLocation,
                    Distance = distance,
                    CumulativeDistance = cumulativeDistance,
                };

                trackings.Add(tracking);

                previousTracking = tracking; // Use the newly created tracking as previous for next iteration

                // Send location update to SignalR clients
                // await hubContext.Clients.All.SendAsync(
                //     "ReceiveLocationUpdate",
                //     booking.Id,
                //     point.Latitude,
                //     point.Longitude,
                //     cancellationToken: cancellationToken
                // );
            }

            context.TripTrackings.AddRange(trackings);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        private Point CreateLocationPoint(LocationPoint point)
        {
            var currentLocation = geometryFactory.CreatePoint(
                new Coordinate((double)point.Longitude, (double)point.Latitude)
            );
            currentLocation.SRID = 4326; // Set SRID for GPS coordinates

            return currentLocation;
        }

        private static (decimal Distance, decimal CumulativeDistance) CalculateDistance(
            Point currentLocation,
            TripTracking? lastTracking
        )
        {
            const decimal metersPerDegree = 111320m;

            if (lastTracking == null)
                return (0, 0);

            var distance =
                (decimal)lastTracking.Location.Distance(currentLocation) * metersPerDegree;
            var cumulativeDistance = lastTracking.CumulativeDistance + distance;

            return (distance, cumulativeDistance);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LocationPoints)
                .NotEmpty()
                .Must(x => x.Any())
                .WithMessage("Không có điểm định vị nào được gửi");
        }
    }
}
