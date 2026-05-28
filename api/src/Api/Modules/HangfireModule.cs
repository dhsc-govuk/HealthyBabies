using Application.Common.Settings;
using Hangfire;
using HangfireBasicAuthenticationFilter;

namespace Api.Modules;

internal static class HangfireModule
{
    internal static void ConfigureHangfire(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<ApplicationSettings>();

        app.UseHangfireDashboard();
        app.MapHangfireDashboard("/background-jobs", new DashboardOptions
        {
            Authorization = new[]
            {
                new HangfireCustomBasicAuthenticationFilter
                {
                    User = settings.Hangfire.User,
                    Pass = settings.Hangfire.Password
                }
            }
        });
    }
}