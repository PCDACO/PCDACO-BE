using Ardalis.Result;

using Domain.Entities;
using Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

namespace UseCases.UC_Car.Commands;

public sealed class UpdateCarStatistic
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            Car? gettingCar = await context.Cars
                .Include(x => x.Bookings).ThenInclude(b => b.TripTrackings)
                .Include(x => x.Bookings).ThenInclude(b => b.Feedbacks)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (gettingCar is null)
                return Result.NotFound($"Không tìm thấy xe với Id : {request.Id}");
            CarStatistic? updatingCarStatistic = await context.CarStatistics
                .FirstOrDefaultAsync(x => x.CarId == request.Id, cancellationToken);
            if (updatingCarStatistic is null)
                return Result.NotFound($"Không tìm thấy xe với Id : {request.Id}");
            // Update
            updatingCarStatistic.TotalRented = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Where(b => !b.IsDeleted)
                .Count();
            updatingCarStatistic.TotalCancellation = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Cancelled)
                .Where(b => !b.IsDeleted)
                .Count();
            updatingCarStatistic.TotalEarning = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Where(b => !b.IsDeleted)
                .Sum(b => b.TotalAmount);
            updatingCarStatistic.TotalDistance = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Where(b => !b.IsDeleted)
                .Sum(b => b.TripTrackings.OrderByDescending(t => t.Id).FirstOrDefault()?.CumulativeDistance ?? 0);
            updatingCarStatistic.AverageRating = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Where(b => !b.IsDeleted)
                .Average(b => b.Feedbacks
                    .Where(f => !f.IsDeleted)
                    .Where(f => f.Type == FeedbackTypeEnum.Owner)
                    .Average(f => (decimal)f.Point));
            updatingCarStatistic.LastRented = gettingCar.Bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.Id)
                .FirstOrDefault()?.EndTime ?? null!;
            // Save
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}