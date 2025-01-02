using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DIConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Add services here
        return services;
    }
}