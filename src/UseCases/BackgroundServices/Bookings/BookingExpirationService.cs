using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UseCases.BackgroundServices.Bookings;

public class BookingExpirationService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Set your desired interval

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var expireBookingsJob =
                    scope.ServiceProvider.GetRequiredService<ExpireBookingsJob>();

                BackgroundJob.Enqueue(() => expireBookingsJob.ExpireOldBookings());
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
