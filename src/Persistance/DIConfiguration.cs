using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistance.Data;

namespace Persistance;

public static class DIConfiguration
{
    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDBContext>(options =>
        {
            options.UseNpgsql(configuration["ConnectionStrings"]);
        }
            );
        return services;
    }
}