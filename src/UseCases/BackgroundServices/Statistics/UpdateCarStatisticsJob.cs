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
                            b.CarId == cs.CarId
                            && b.Status.Name == BookingStatusEnum.Completed.ToString()
                        )
                )
                .SetProperty(
                    cs => cs.TotalRejected,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId
                            && b.Status.Name == BookingStatusEnum.Rejected.ToString()
                        )
                )
                .SetProperty(
                    cs => cs.TotalExpired,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId
                            && b.Status.Name == BookingStatusEnum.Expired.ToString()
                        )
                )
                .SetProperty(
                    cs => cs.TotalCancelled,
                    cs =>
                        context.Bookings.Count(b =>
                            b.CarId == cs.CarId
                            && b.Status.Name == BookingStatusEnum.Cancelled.ToString()
                        )
                )
                .SetProperty(
                    cs => cs.TotalEarning,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                            )
                            .Sum(b => (decimal?)b.TotalAmount) ?? 0
                )
                .SetProperty(
                    cs => cs.TotalDistance,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                            )
                            .Sum(b => (decimal?)b.TripTrackings.Sum(t => t.Distance)) ?? 0
                )
                .SetProperty(
                    cs => cs.AverageRating,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                            )
                            .Average(b => (decimal?)b.Feedbacks.Average(f => f.Point)) ?? 0
                )
                .SetProperty(
                    cs => cs.LastRented,
                    cs =>
                        context
                            .Bookings.Where(b =>
                                b.CarId == cs.CarId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                            )
                            .Max(b => (DateTimeOffset?)b.EndTime) ?? null
                )
        );
    }
}
