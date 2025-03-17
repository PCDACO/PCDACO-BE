using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Persistance.Data;
using UseCases.Abstractions;

namespace Persistance;

public static class DIConfiguration
{
    public static IServiceCollection AddPersistance(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration["CONNECTION_STRING"]);
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<IAppDBContext, AppDBContext>(options =>
        {
            options.UseNpgsql(
                dataSource,
                o =>
                {
                    o.UseNetTopologySuite();
                }
            );
        });

        return services;
    }
}
