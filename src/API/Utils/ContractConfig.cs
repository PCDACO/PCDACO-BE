using Domain.Shared;

namespace API.Utils;

public static class ContractConfig
{
    public static IServiceCollection AddContractService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var collateralPrice = GetRequiredConfig(configuration, "COLLATERAL_PRICE");

        ContractSettings contractSettings = new() { CollateralPrice = int.Parse(collateralPrice) };

        services.AddSingleton(s => contractSettings);

        return services;
    }

    private static string GetRequiredConfig(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value)
            ? throw new ConfigurationMissingException(key)
            : value;
    }
}
