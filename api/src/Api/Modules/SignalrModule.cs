using Application.SignalR;

namespace Api.Modules;

internal static class SignalrModule
{
    internal static void ConfigureSignalR(this WebApplication app)
    {
        app.MapHub<SignalrHub>("/hub").RequireAuthorization();
    }
}