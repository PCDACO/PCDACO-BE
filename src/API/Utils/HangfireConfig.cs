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
        services.AddScoped<BookingReminderJob>();

        return services;
    }
}
