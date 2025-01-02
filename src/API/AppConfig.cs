using Carter;

namespace API;

public static class AppConfig
{
    public static WebApplication AddAppConfig(this WebApplication app)
    {
        app.MapCarter();
        return app;
    }
}