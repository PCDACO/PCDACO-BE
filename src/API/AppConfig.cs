using Carter;

namespace API;

public static class AppConfig
{
    public static WebApplication AddAppConfig(this WebApplication app)
    {
        app.MapCarter();
        app.UseExceptionHandler();
        app.UseCors("AllowAll");
        return app;
    }
}