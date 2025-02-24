using Domain.Entities;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using UseCases.Abstractions;
namespace UseCases.BackgroundServices.BS_CarStatistics;

public class UpdateCarStatisticsBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // await Task.Delay(TimeSpan.FromMinutes(10),stoppingToken);
            using var scope = scopeFactory.CreateScope();
            IAppDBContext context = scope.ServiceProvider.GetRequiredService<IAppDBContext>();
            int pageNumber = 1;
            const int pageSize = 10;
            IQueryable<Car> carQuery = context.Cars
                .AsNoTracking()
                .Include(x => x.Bookings).ThenInclude(b => b.TripTrackings)
                .Include(x => x.Bookings).ThenInclude(b => b.Feedbacks)
                .Include(x => x.Bookings).ThenInclude(b => b.Status)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.Id);
            Console.WriteLine("HII");
            while (true)
            {
                IEnumerable<Car> gettingCars = await carQuery
                    .Skip((pageNumber++ - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(stoppingToken);
                if (!gettingCars.Any()) break;
                // // Get status ids
                Guid? completedStatusId = await context.BookingStatuses
                    .Where(x => EF.Functions.ILike(x.Name, "%completed%"))
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(stoppingToken);
                if (completedStatusId is null) continue;
                Guid? cancelledStatusId = await context.BookingStatuses
                    .Where(x => EF.Functions.ILike(x.Name, "%cancelled%"))
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(stoppingToken);
                if (cancelledStatusId is null) continue;
                // // Update
                foreach (var car in gettingCars)
                {
                    CarStatistic? updatingCarStatisic = await context.CarStatistics
                        .Where(cs => cs.CarId == car.Id)
                        .Where(cs => cs.IsDeleted)
                        .FirstOrDefaultAsync(stoppingToken);
                    if (updatingCarStatisic is null) continue;
                    if (car.CarStatistic is null)
                    {
                        CarStatistic addingCarStatistic = new()
                        {
                            CarId = car.Id,
                            TotalRented = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Count(),
                            TotalCancellation = car.Bookings
                            .Where(b => b.Status.Id == cancelledStatusId)
                            .Where(b => !b.IsDeleted)
                            .Count(),
                            TotalEarning = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Sum(b => b.TotalAmount),
                            TotalDistance = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Sum(b => b.TripTrackings.OrderByDescending(t => t.Id).FirstOrDefault()?.CumulativeDistance ?? 0),
                            AverageRating = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Average(b => b.Feedbacks
                                .Where(f => f.Type == FeedbackTypeEnum.Owner)
                                .Average(f => (decimal)f.Point)),
                            LastRented = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .OrderByDescending(b => b.Id)
                            .FirstOrDefault()?.EndTime ?? null!,
                        };
                        await context.CarStatistics.AddAsync(addingCarStatistic, stoppingToken);
                    }
                    else
                    {
                        updatingCarStatisic.TotalRented = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Count();
                        updatingCarStatisic.TotalCancellation = car.Bookings
                            .Where(b => b.Status.Id == cancelledStatusId)
                            .Where(b => !b.IsDeleted)
                            .Count();
                        updatingCarStatisic.TotalEarning = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Sum(b => b.TotalAmount);
                        updatingCarStatisic.TotalDistance = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Sum(b => b.TripTrackings.OrderByDescending(t => t.Id).FirstOrDefault()?.CumulativeDistance ?? 0);
                        updatingCarStatisic.AverageRating = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .Average(b => b.Feedbacks
                                .Where(f => f.Type == FeedbackTypeEnum.Owner)
                                .Average(f => (decimal)f.Point));
                        updatingCarStatisic.LastRented = car.Bookings
                            .Where(b => b.Status.Id == completedStatusId)
                            .Where(b => !b.IsDeleted)
                            .OrderByDescending(b => b.Id)
                            .FirstOrDefault()?.EndTime ?? null!;
                    }
                    // Save
                    await context.SaveChangesAsync(stoppingToken);
                    await Task.Delay(4 * 1000, stoppingToken);
                }
            }
            await Task.Delay(10 * 1000, stoppingToken);
        }
    }
}