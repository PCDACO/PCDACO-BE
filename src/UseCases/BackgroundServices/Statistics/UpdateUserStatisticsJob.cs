using Domain.Constants.EntityNames;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.BackgroundServices.Statistics;

public class UpdateUserStatisticsJob(IAppDBContext context)
{
    public async Task UpdateUserStatistic()
    {
        await context.UserStatistics
        .Include(us => us.User)
        .ThenInclude(u => u.Role)
        .ExecuteUpdateAsync(setter =>
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
                            .Bookings.Include(b => b.Car)
                            .ThenInclude(c => c.Owner)
                            .Where(b =>
                                b.Car.Owner.Id == us.UserId
                                && b.Status.Name == BookingStatusEnum.Completed.ToString()
                                && us.User.Role.Name == UserRoleNames.Owner
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
                            .SelectMany(b => b.Feedbacks)
                            .Where(f => f.Type == FeedbackTypeEnum.Owner)
                            .Average(f => (decimal?)f.Point) ?? 0
                )
                .SetProperty(
                    us => us.TotalCreatedInspectionSchedule,
                    us =>
                        context.InspectionSchedules.Count(s =>
                            s.CreatedBy == us.UserId && !s.IsDeleted
                        )
                )
                .SetProperty(
                    us => us.TotalApprovedInspectionSchedule,
                    us =>
                        context.InspectionSchedules.Count(s =>
                            s.TechnicianId == us.UserId
                            && s.InspectionStatus.Name == InspectionStatusNames.Approved.ToString()
                            && !s.IsDeleted
                        )
                )
                .SetProperty(
                    us => us.TotalRejectedInspectionSchedule,
                    us =>
                        context.InspectionSchedules.Count(s =>
                            s.TechnicianId == us.UserId
                            && s.InspectionStatus.Name == InspectionStatusNames.Rejected.ToString()
                            && !s.IsDeleted
                        )
                )
        );
    }
}
