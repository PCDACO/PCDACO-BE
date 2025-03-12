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
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class TrackTripLocation
{
    public sealed record Command(Guid BookingId, decimal Latitude, decimal Longitude)
        : IRequest<Result>;

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

            if (booking.Status != BookingStatusEnum.Ongoing)
                return Result.Error("Chỉ có thể cập nhật vị trí khi chuyến đi đang diễn ra");

            // Create current location point
            var currentLocation = geometryFactory.CreatePoint(
                new Coordinate((double)request.Longitude, (double)request.Latitude)
            );
            currentLocation.SRID = 4326; // Set SRID for GPS coordinates

            // Calculate distance from last tracking point
            decimal distance = 0;
            decimal cumulativeDistance = 0;

            var lastTracking = booking.TripTrackings.OrderByDescending(t => t.Id).FirstOrDefault();

            if (lastTracking != null)
            {
                // Calculate distance in meters (convert from degrees to meters)
                const decimal metersPerDegree = 111320m;
                distance =
                    (decimal)lastTracking.Location.Distance(currentLocation) * metersPerDegree;
                cumulativeDistance = lastTracking.CumulativeDistance + distance;
            }

            var tracking = new TripTracking
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                BookingId = booking.Id,
                Location = currentLocation,
                Distance = distance,
                CumulativeDistance = cumulativeDistance,
            };

            context.TripTrackings.Add(tracking);
            await context.SaveChangesAsync(cancellationToken);

            // Send location update to SignalR clients
            await hubContext.Clients.All.SendAsync(
                "ReceiveLocationUpdate",
                booking.Id,
                request.Latitude,
                request.Longitude,
                cancellationToken: cancellationToken
            );

            return Result.Success();
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