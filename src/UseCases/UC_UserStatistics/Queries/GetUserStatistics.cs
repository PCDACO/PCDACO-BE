using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_UserStatistic.Queries;

public sealed class GetUserStatistics
{
    public sealed record Query() : IRequest<Result<Response>>;

    public sealed record Response(
        int TotalBooking,
        int TotalCompleted,
        int TotalRejected,
        int TotalExpired,
        int TotalCancelled,
        decimal TotalEarning,
        decimal AverageRating,
        int TotalCreatedInspectionSchedule,
        int TotalApprovedInspectionSchedule,
        int TotalRejectedInspectionSchedule
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get the current user with role
            var user = await context
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            // Calculate booking statistics
            int totalBooking =
                user.Role.Name == UserRoleNames.Driver
                    ? await context.Bookings.CountAsync(
                        b => b.UserId == user.Id && !b.IsDeleted,
                        cancellationToken
                    )
                    : 0;

            int totalCompleted =
                user.Role.Name == UserRoleNames.Driver
                    ? await context
                        .Bookings.Where(b => b.UserId == user.Id && !b.IsDeleted)
                        .CountAsync(
                            b => b.Status == BookingStatusEnum.Completed,
                            cancellationToken
                        )
                    : 0;

            int totalRejected =
                user.Role.Name == UserRoleNames.Driver
                    ? await context
                        .Bookings.Where(b => b.UserId == user.Id && !b.IsDeleted)
                        .CountAsync(
                            b => b.Status == BookingStatusEnum.Rejected,
                            cancellationToken
                        )
                    : 0;

            int totalExpired =
                user.Role.Name == UserRoleNames.Driver
                    ? await context
                        .Bookings.Where(b => b.UserId == user.Id && !b.IsDeleted)
                        .CountAsync(
                            b => b.Status == BookingStatusEnum.Expired,
                            cancellationToken
                        )
                    : 0;

            int totalCancelled =
                user.Role.Name == UserRoleNames.Driver
                    ? await context
                        .Bookings.Where(b => b.UserId == user.Id && !b.IsDeleted)
                        .CountAsync(
                            b => b.Status == BookingStatusEnum.Cancelled,
                            cancellationToken
                        )
                    : 0;

            // Calculate total earnings (for owner only)
            decimal totalEarning = 0;
            if (user.Role.Name == UserRoleNames.Owner)
            {
                totalEarning = await context
                    .Bookings.Where(b => !b.IsDeleted)
                    .Include(b => b.Car)
                    .Where(b =>
                        b.Car.OwnerId == user.Id
                        && b.Status == BookingStatusEnum.Completed
                    )
                    .SumAsync(b => b.BasePrice, cancellationToken);
            }

            // Calculate average rating for driver or owner
            decimal averageRating = 0;
            if (user.Role.Name == UserRoleNames.Driver || user.Role.Name == UserRoleNames.Owner)
            {
                var feedbacks = await context
                    .Feedbacks.Where(f => !f.IsDeleted)
                    .Include(f => f.Booking)
                    .ToListAsync(cancellationToken);

                if (user.Role.Name == UserRoleNames.Driver)
                {
                    feedbacks = feedbacks
                        .Where(f =>
                            f.Booking.UserId == user.Id
                            && f.Booking.Status == BookingStatusEnum.Completed
                            && f.Type == FeedbackTypeEnum.Owner
                        )
                        .ToList();
                }
                if (user.Role.Name == UserRoleNames.Owner)
                {
                    feedbacks = feedbacks
                        .Where(f =>
                            f.Booking.Car.OwnerId == user.Id
                            && f.Booking.Status == BookingStatusEnum.Completed
                            && f.Type == FeedbackTypeEnum.Driver
                        )
                        .ToList();
                }

                if (feedbacks.Any())
                {
                    averageRating = feedbacks.Average(f => (decimal)f.Point);
                }
            }

            // Calculate inspection schedule statistics
            int totalCreatedInspectionSchedule =
                user.Role.Name == UserRoleNames.Consultant
                    ? await context.InspectionSchedules.CountAsync(
                        s => s.CreatedBy == user.Id && !s.IsDeleted,
                        cancellationToken
                    )
                    : 0;

            int totalApprovedInspectionSchedule =
                user.Role.Name == UserRoleNames.Technician
                    ? await context
                        .InspectionSchedules.Where(s => s.TechnicianId == user.Id && !s.IsDeleted)
                        .CountAsync(
                            s => s.Status == InspectionScheduleStatusEnum.Approved,
                            cancellationToken
                        )
                    : 0;

            int totalRejectedInspectionSchedule =
                user.Role.Name == UserRoleNames.Technician
                    ? await context
                        .InspectionSchedules.Where(s => s.TechnicianId == user.Id && !s.IsDeleted)
                        .CountAsync(
                            s => s.Status == InspectionScheduleStatusEnum.Rejected,
                            cancellationToken
                        )
                    : 0;

            // Create response with calculated statistics
            var response = new Response(
                TotalBooking: totalBooking,
                TotalCompleted: totalCompleted,
                TotalRejected: totalRejected,
                TotalExpired: totalExpired,
                TotalCancelled: totalCancelled,
                TotalEarning: totalEarning,
                AverageRating: averageRating,
                TotalCreatedInspectionSchedule: totalCreatedInspectionSchedule,
                TotalApprovedInspectionSchedule: totalApprovedInspectionSchedule,
                TotalRejectedInspectionSchedule: totalRejectedInspectionSchedule
            );

            return Result.Success(response, ResponseMessages.Fetched);
        }
    }
}