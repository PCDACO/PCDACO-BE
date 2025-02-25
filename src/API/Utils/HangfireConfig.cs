using Hangfire;
using Hangfire.PostgreSql;
using UseCases.BackgroundServices.Bookings;

namespace API.Utils;

public static class HangfireConfig
{
    public static IServiceCollection AddHangFireService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(configuration["CONNECTION_STRING"])
        );

        services.AddHangfireServer();

        // Register the job service
        services.AddScoped<BookingExpiredJob>();

        // Schedule recurring jobs at startup
        using var serviceProvider = services.BuildServiceProvider();
        var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<BookingExpiredJob>(
            "expire-old-bookings",
            job => job.ExpireOldBookings(),
            Cron.Daily // Runs every day at midnight
        );

        return services;
    }
}
