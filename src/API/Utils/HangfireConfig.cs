using Hangfire;
using Hangfire.PostgreSql;
using UseCases.BackgroundServices.Bookings;
using UseCases.BackgroundServices.Statistics;

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
        services.AddScoped<BookingReminderJob>();
        services.AddScoped<UpdateCarStatisticsJob>();
        services.AddScoped<UpdateUserStatisticsJob>();

        return services;
    }

    public static void RegisterRecurringJob()
    {
        RecurringJob.AddOrUpdate<UpdateCarStatisticsJob>(
            "update-car-statistics",
            job => job.UpdateCarStatistic(),
            Cron.Hourly
        );

        RecurringJob.AddOrUpdate<UpdateUserStatisticsJob>(
            "update-user-statistics",
            job => job.UpdateUserStatistic(),
            Cron.Hourly
        );

        RecurringJob.AddOrUpdate<BookingExpiredJob>(
            "expire-bookings-automatically",
            job => job.ExpireBookingsAutomatically(),
            Cron.Daily
        );
    }
}
