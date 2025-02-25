using Hangfire;
using Hangfire.PostgreSql;

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

        return services;
    }
}
