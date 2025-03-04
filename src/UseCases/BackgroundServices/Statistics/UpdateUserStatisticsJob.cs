using Domain.Constants.EntityNames;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Statistics;

public class UpdateUserStatisticsJob(IAppDBContext context)
{
    public async Task UpdateUserStatistic()
    {
        await context.UserStatistics.ExecuteUpdateAsync(setter =>
            setter
                .SetProperty(
                    us => us.TotalBooking,
                    us => context.Bookings.Count(b => b.UserId == us.UserId)
                )
                .SetProperty(
                    us => us.TotalCompleted,
                    us =>
                        context.Bookings.Count(b =>
                            b.UserId == us.UserId
                            && b.Status.Name == BookingStatusEnum.Completed.ToString()
                        )
                )
                .SetProperty(
                    us => us.TotalRejected,
                    us =>
                        context.Bookings.Count(b =>
                            b.UserId == us.UserId
                            && b.Status.Name == BookingStatusEnum.Rejected.ToString()
                        )
                )
                .SetProperty(
                    us => us.TotalExpired,
                    us =>
                        context.Bookings.Count(b =>
                            b.UserId == us.UserId
                            && b.Status.Name == BookingStatusEnum.Expired.ToString()
                        )
                )
                .SetProperty(
                    us => us.TotalCancelled,
                    us =>
                        context.Bookings.Count(b =>
                            b.UserId == us.UserId
                            && b.Status.Name == BookingStatusEnum.Cancelled.ToString()
                        )
                )
                .SetProperty(
                    us => us.TotalEarning,
                    us =>
                        context
                            .Bookings.Where(b =>
                                b.UserId == us.UserId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                                && b.User.Role.Name == UserRoleNames.Owner
                            )
                            .Sum(b => (decimal?)b.BasePrice) ?? 0
                )
                .SetProperty(
                    us => us.AverageRating,
                    us =>
                        context
                            .Bookings.Where(b =>
                                b.UserId == us.UserId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                            )
                            .Average(b => (decimal?)b.Feedbacks.Average(f => f.Point)) ?? 0
                )
        );
    }
}
