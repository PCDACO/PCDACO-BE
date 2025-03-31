using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Statistics.Queries;

public sealed class GetTotalStatistics
{
    public sealed record Query() : IRequest<Result<Response>>;

    public sealed record TimeSeriesData(string Month, decimal Value);

    public sealed record Response(
        decimal TotalRevenue,
        int ActiveUsers,
        int ActiveTransactions,
        int TotalRentedCars,
        int TotalBookingCancelled,
        decimal CancellationLoss,
        List<TimeSeriesData> RevenueOverTime,
        List<TimeSeriesData> ActiveUsersOverTime
    );

    internal sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Calculate total revenue (from completed bookings)
            decimal totalRevenue = await context
                .Bookings.Where(b => b.Status == BookingStatusEnum.Completed && !b.IsDeleted)
                .SumAsync(b => b.TotalAmount, cancellationToken);

            // Count active users
            int totalActiveUsers = context
                .Bookings.Include(b => b.User)
                .Select(b => b.UserId)
                .Distinct()
                .Count();

            // Count active transactions
            int activeTransactions = await context
                .Transactions.Where(t =>
                    (
                        t.Status == TransactionStatusEnum.Pending
                        || t.Status == TransactionStatusEnum.Completed
                    ) && !t.IsDeleted
                )
                .CountAsync(cancellationToken);

            // Count total rented cars (cars that have been in at least one completed booking)
            int totalRentedCars = context
                .Bookings.AsNoTracking()
                .Include(b => b.Car)
                .Where(b => b.Status == BookingStatusEnum.Completed && !b.IsDeleted)
                .Select(b => b.CarId)
                .Distinct()
                .Count();

            // Count total cancelled bookings
            int totalBookingCancelled = await context
                .Bookings.Where(b => b.Status == BookingStatusEnum.Cancelled && !b.IsDeleted)
                .CountAsync(cancellationToken);

            // Calculate total loss from cancelled bookings
            decimal cancellationLoss = await context
                .Bookings.Where(b => b.Status == BookingStatusEnum.Cancelled && !b.IsDeleted)
                .SumAsync(b => b.TotalAmount, cancellationToken);

            // Calculate revenue over time and active user over time (last 12 months)
            var currentDate = DateTimeOffset.UtcNow;
            var revenueOverTime = new List<TimeSeriesData>();
            var activeUsersOverTime = new List<TimeSeriesData>();

            string[] monthNames =
            {
                "Jan",
                "Feb",
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
            };

            // Get last 12 months of data
            for (int i = 11; i >= 0; i--)
            {
                var targetMonth = currentDate.AddMonths(-i);
                var startOfMonth = new DateTimeOffset(
                    targetMonth.Year,
                    targetMonth.Month,
                    1,
                    0,
                    0,
                    0,
                    TimeSpan.Zero
                );
                var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);

                // Calculate revenue for this month
                var monthlyRevenue = await context
                    .Bookings.Where(b =>
                        b.Status == BookingStatusEnum.Completed
                        && !b.IsDeleted
                        && b.ActualReturnTime >= startOfMonth
                        && b.ActualReturnTime <= endOfMonth
                    )
                    .SumAsync(b => b.TotalAmount, cancellationToken);

                // Count active users for this month
                var monthlyActiveUsers = context
                    .Bookings.AsNoTracking()
                    .Include(b => b.User)
                    .AsEnumerable()
                    .Where(b =>
                        GetTimestampFromUuid.Execute(b.Id) >= startOfMonth
                        && GetTimestampFromUuid.Execute(b.Id) <= endOfMonth
                    )
                    .Select(b => b.UserId)
                    .Distinct()
                    .Count();

                // Add to the time series data
                var monthName = monthNames[startOfMonth.Month - 1]; // Month names are 1-indexed
                revenueOverTime.Add(new TimeSeriesData(monthName, monthlyRevenue));
                activeUsersOverTime.Add(new TimeSeriesData(monthName, monthlyActiveUsers));
            }

            var response = new Response(
                TotalRevenue: totalRevenue,
                ActiveUsers: totalActiveUsers,
                ActiveTransactions: activeTransactions,
                TotalRentedCars: totalRentedCars,
                TotalBookingCancelled: totalBookingCancelled,
                CancellationLoss: cancellationLoss,
                RevenueOverTime: revenueOverTime,
                ActiveUsersOverTime: activeUsersOverTime
            );

            return Result.Success(response, "Lấy thống kê hệ thống thành công");
        }
    }
}
