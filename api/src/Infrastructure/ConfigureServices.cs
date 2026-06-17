using Application.Common.Interfaces;
using Infrastructure.Blob;
using Infrastructure.Caching;
using Infrastructure.DateTimeProvider;
using Infrastructure.Graph;
using Infrastructure.Hangfire;
using Infrastructure.Identity;
using Infrastructure.Mailing;
using Infrastructure.Mfa;
using Infrastructure.OsPlaces;
using Infrastructure.Persistence;
using Infrastructure.RequestStaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureServices
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InMemoryCache>();
        services.AddSingleton<IInMemoryCache>(provider => provider.GetRequiredService<InMemoryCache>());
        services.AddPersistenceServices(configuration);
        services.AddGraphServices();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<SystemDateTimeProvider>();
        services.AddSingleton<IDateTimeProvider>(provider => provider.GetRequiredService<SystemDateTimeProvider>());
        services.AddBlobServices();
        services.AddOsPlacesServices();
        services.AddHangfireServices(configuration);
        services.AddScoped<IRequestStagingService, RequestStagingService>();
        services.AddScoped<RequestStagingCleanupJob>();
        services.AddScoped<IEmailNotificationService, AzureCommunicationEmailService>();

        // MFA Services
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IMfaEncryptionService, MfaEncryptionService>();
        services.AddScoped<IMfaRateLimitService, MfaRateLimitService>();
        services.AddScoped<IMfaSessionService, MfaSessionService>();
        services.AddScoped<IRecoveryCodeService, RecoveryCodeService>();
    }
}