using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Api.Telemetry;

public class ReleaseTagInitializer : ITelemetryInitializer
{
    private static readonly string Release =
        Environment.GetEnvironmentVariable("APP_VERSION") ?? "local";

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "family-hubs-api";
        telemetry.Context.GlobalProperties.TryAdd("release", Release);
    }
}