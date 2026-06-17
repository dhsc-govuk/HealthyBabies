using Application.Common.Constants;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.RequestStaging;

public class RequestStagingCleanupJob(
    IRequestStagingService requestStagingService,
    ILogger<RequestStagingCleanupJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var maxAge = TimeSpan.FromHours(RequestStagingConstants.MaxAgeHours);
        var result = await requestStagingService.CleanupExpiredAsync(maxAge, cancellationToken);

        result.Match(
            deleted => logger.LogInformation(
                "Request-staging cleanup removed {Deleted} expired blob(s)",
                deleted),
            ex => logger.LogError(ex, "Request-staging cleanup failed"));
    }
}