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
        List<TimeSeriesData> ActiveUsersOverTime,
        List<TimeSeriesData> BookingsOverTime,
        List<TimeSeriesData> ActiveCarsOverTime
    );

    internal sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Calculate total revenue (from completed or done bookings)
            decimal totalRevenue = await context
                .Bookings.Where(b =>
                    (b.Status == BookingStatusEnum.Done || b.Status == BookingStatusEnum.Completed)
                    && !b.IsDeleted
                )
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

            // Count total rented cars
            int totalRentedCars = context
                .Bookings.AsNoTracking()
                .Include(b => b.Car)
                .Where(b =>
                    b.Status != BookingStatusEnum.Expired
                    && b.Status != BookingStatusEnum.Cancelled
                    && b.Status != BookingStatusEnum.Rejected
                    && b.Status != BookingStatusEnum.Pending
                    && !b.IsDeleted
                )
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
            DateTimeOffset currentDate = DateTimeOffset.UtcNow;
            List<TimeSeriesData> revenueOverTime = [];
            List<TimeSeriesData> activeUsersOverTime = [];
            List<TimeSeriesData> bookingsOverTime = [];
            List<TimeSeriesData> carsOverTime = [];

            string[] monthNames =
            {
                "T1",
                "T2",
                "T3",
                "T4",
                "T5",
                "T6",
                "T7",
                "T8",
                "T9",
                "T10",
                "T11",
                "T12",
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
                DateTimeOffset endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);

                // Calculate revenue for this month
                decimal monthlyRevenue = await context
                    .Bookings.Where(b =>
                        (
                            b.Status == BookingStatusEnum.Completed
                            || b.Status == BookingStatusEnum.Done
                        )
                        && !b.IsDeleted
                        && b.ActualReturnTime >= startOfMonth
                        && b.ActualReturnTime <= endOfMonth
                    )
                    .SumAsync(b => b.TotalAmount, cancellationToken);

                // Count active users for this month
                int monthlyActiveUsers = context
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

                // Count completed bookings for this month
                int monthlyBookings = context
                    .Bookings.AsNoTracking()
                    .AsEnumerable()
                    .Where(b =>
                        GetTimestampFromUuid.Execute(b.Id) >= startOfMonth
                        && GetTimestampFromUuid.Execute(b.Id) <= endOfMonth
                    )
                    .Where(b =>
                        (
                            b.Status == BookingStatusEnum.Completed
                            || b.Status == BookingStatusEnum.Done
                        )
                    )
                    .Select(b => b.Id)
                    .Distinct()
                    .Count();

                // Count completed bookings for this month
                int monthlyCars = context
                    .Cars.AsNoTracking()
                    .AsEnumerable()
                    .Where(b =>
                        GetTimestampFromUuid.Execute(b.Id) >= startOfMonth
                        && GetTimestampFromUuid.Execute(b.Id) <= endOfMonth
                    )
                    .Where(c => c.Status == CarStatusEnum.Available)
                    .Select(c => c.Id)
                    .Distinct()
                    .Count();

                // Add to the time series data
                string monthName = monthNames[startOfMonth.Month - 1]; // Month names are 1-indexed
                revenueOverTime.Add(new TimeSeriesData(monthName, monthlyRevenue));
                activeUsersOverTime.Add(new TimeSeriesData(monthName, monthlyActiveUsers));
                bookingsOverTime.Add(new TimeSeriesData(monthName, monthlyBookings));
                carsOverTime.Add(new TimeSeriesData(monthName, monthlyCars));
            }

            return Result.Success(
                value: new Response(
                    TotalRevenue: totalRevenue,
                    ActiveUsers: totalActiveUsers,
                    ActiveTransactions: activeTransactions,
                    TotalRentedCars: totalRentedCars,
                    TotalBookingCancelled: totalBookingCancelled,
                    CancellationLoss: cancellationLoss,
                    RevenueOverTime: revenueOverTime,
                    ActiveUsersOverTime: activeUsersOverTime,
                    BookingsOverTime: bookingsOverTime,
                    ActiveCarsOverTime: carsOverTime
                ),
                successMessage: "Lấy thống kê hệ thống thành công"
            );
        }
    }
}
