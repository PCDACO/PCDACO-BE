using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Persistance.Data;

using UseCases.Abstractions;

namespace Persistance;

public static class DIConfiguration
{
    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IAppDBContext, AppDBContext>(options => options.UseNpgsql(configuration["CONNECTION_STRING"]));
        return services;
    }
}