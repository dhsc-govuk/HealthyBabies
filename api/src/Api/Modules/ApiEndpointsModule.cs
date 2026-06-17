using Microsoft.AspNetCore.Authorization;

namespace Api.Modules;

internal static class ApiEndpointsModule
{
    internal static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var startupTime = DateTime.Now;
        app.MapGet("/", () => string.Empty);
        app.MapGet("/health", () => "healthy");
        app.MapGet("/uptime", [Authorize(Roles = "admin")] () =>
        {
            var current = DateTime.Now;
            var up = current - startupTime;
            return up.ToString();
        });
        return app;
    }
}