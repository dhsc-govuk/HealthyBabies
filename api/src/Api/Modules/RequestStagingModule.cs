using Hangfire;
using Infrastructure.RequestStaging;

namespace Api.Modules;

internal static class RequestStagingModule
{
    internal static void ConfigureRequestStagingCleanup(this WebApplication app)
    {
        try
        {
            RecurringJob.AddOrUpdate<RequestStagingCleanupJob>(
                "request-staging-cleanup",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(2));
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(
                ex,
                "Failed to schedule request-staging cleanup job (likely test environment without persistent Hangfire storage)");
        }
    }
}