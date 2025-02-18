using Domain.Shared;
using Infrastructure.EmailService;
using UseCases.Services.EmailService;

namespace API.Utils;

public static class EmailConfig
{
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Validate and get PayOS configuration
        var mail = GetRequiredConfig(configuration, "MAIL_SETTINGS_MAIL");
        var displayName = GetRequiredConfig(configuration, "MAIL_SETTINGS_DISPLAY_NAME");
        var password = GetRequiredConfig(configuration, "MAIL_SETTINGS_PASSWORD");
        var host = GetRequiredConfig(configuration, "MAIL_SETTINGS_HOST");
        var port = GetRequiredConfig(configuration, "MAIL_SETTINGS_PORT");

        MailSettings mailSettings =
            new()
            {
                Mail = mail,
                DisplayName = displayName,
                Password = password,
                Host = host,
                Port = int.Parse(port)
            };

        services.AddSingleton(s => mailSettings);

        services.AddScoped<IEmailService, EmailService>();

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
