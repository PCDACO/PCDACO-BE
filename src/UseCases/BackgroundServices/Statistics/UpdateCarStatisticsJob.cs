using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Statistics;

public class UpdateCarStatisticsJob(IAppDBContext context)
{
    public async Task UpdateCarStatistic()
    {
        await context.CarStatistics.ExecuteUpdateAsync(setter =>
            setter
                .SetProperty(
                    cs => cs.TotalBooking,
                    cs => context.Bookings.Count(b => b.CarId == cs.CarId)
                )
                .SetProperty(
                    cs => cs.TotalCompleted,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId && b.Status == BookingStatusEnum.Completed
                        )
                )
                .SetProperty(
                    cs => cs.TotalRejected,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId && b.Status == BookingStatusEnum.Rejected
                        )
                )
                .SetProperty(
                    cs => cs.TotalExpired,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId && b.Status == BookingStatusEnum.Expired
                        )
                )
                .SetProperty(
                    cs => cs.TotalCancelled,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId && b.Status == BookingStatusEnum.Cancelled
                        )
                )
                .SetProperty(
                    cs => cs.TotalEarning,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && (
                                    b.Status == BookingStatusEnum.Completed
                                    || b.Status == BookingStatusEnum.Done
                                )
                            )
                            .Sum(b => (decimal?)b.TotalAmount) ?? 0
                )
                .SetProperty(
                    cs => cs.TotalDistance,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && (
                                    b.Status == BookingStatusEnum.Completed
                                    || b.Status == BookingStatusEnum.Done
                                )
                            )
                            .Sum(b => (decimal?)b.TripTrackings.Sum(t => t.Distance)) ?? 0
                )
                .SetProperty(
                    cs => cs.AverageRating,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && (
                                    b.Status == BookingStatusEnum.Completed
                                    || b.Status == BookingStatusEnum.Done
                                )
                            )
                            .SelectMany(b => b.Feedbacks)
                            .Where(f => f.Type == FeedbackTypeEnum.ToOwner)
                            .Average(f => (decimal?)f.Point) ?? 0
                )
                .SetProperty(
                    cs => cs.LastRented,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && (
                                    b.Status == BookingStatusEnum.Done
                                    || b.Status == BookingStatusEnum.Completed
                                )
                            )
                            .Max(b => (DateTimeOffset?)b.ActualReturnTime) ?? null
                )
        );
    }
}
