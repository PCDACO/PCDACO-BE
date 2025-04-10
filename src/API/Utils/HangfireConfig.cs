using Hangfire;
using Hangfire.PostgreSql;
using UseCases.BackgroundServices.Bookings;
using UseCases.BackgroundServices.InspectionSchedule;
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
        services.AddScoped<BookingOverdueJob>();
        services.AddScoped<UpdateCarStatisticsJob>();
        services.AddScoped<UpdateUserStatisticsJob>();
        services.AddScoped<InspectionScheduleExpiredJob>();

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
            "*/15 * * * *" // Run every 15 minutes
        );

        RecurringJob.AddOrUpdate<BookingOverdueJob>(
            "handle-overdue-bookings",
            job => job.HandleOverdueBookings(),
            "*/15 * * * *" // Run every 15 minutes
        );

        RecurringJob.AddOrUpdate<InspectionScheduleExpiredJob>(
            "expire-inspection-schedules-automatically",
            job => job.ExpireInspectionSchedulesAutomatically(),
            Cron.Minutely
        );
    }
}
