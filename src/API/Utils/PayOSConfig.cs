using Infrastructure.PayOSService;

using Net.payOS;
using UseCases.DTOs;
using UseCases.Services.PayOSService;

namespace API.Utils;

public class ConfigurationMissingException : InvalidOperationException
{
    public ConfigurationMissingException(string configKey)
        : base($"Required configuration key '{configKey}' is missing") { }
}

public static class PayOSConfig
{
    public static IServiceCollection AddPayOSService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Validate and get PayOS configuration
        var clientId = GetRequiredConfig(configuration, "PAYOS_CLIENT_ID");
        var apiKey = GetRequiredConfig(configuration, "PAYOS_API_KEY");
        var checkSumKey = GetRequiredConfig(configuration, "PAYOS_CHECKSUM_KEY");

        PayOS payOS = new(clientId, apiKey, checkSumKey);
        services.AddSingleton(s => payOS);

        // Validate and get URL settings
        var returnUrl = GetRequiredConfig(configuration, "RETURN_URL");
        var cancelUrl = GetRequiredConfig(configuration, "CANCEL_URL");

        UrlSettings urlSettings = new() { ReturnUrl = returnUrl, CancelUrl = cancelUrl };
        services.AddSingleton(s => urlSettings);

        services.AddScoped<IPaymentService, PayOSService>();

        return services;
    }

    private static string GetRequiredConfig(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new ConfigurationMissingException(key);

        return value;
    }
}
