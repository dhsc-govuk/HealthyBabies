using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Users.Interfaces;
using Hangfire;
using Infrastructure.Blob;
using Infrastructure.Caching;
using Infrastructure.Graph;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Tests.Common.Services;
using Xunit;

namespace Tests.Common;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly FakeMemoryCache _fakeCache;
    private readonly InMemoryBlobService _blobService;

    public IntegrationTestWebFactory()
    {
        _fakeCache = new FakeMemoryCache();
        _blobService = new InMemoryBlobService();
    }

    public FakeMemoryCache FakeCache => _fakeCache;

    public InMemoryBlobService BlobService => _blobService;
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("neur-eye-tbo-db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config
                .AddJsonFile("appsettings.Test.json")
                .AddEnvironmentVariables();
        }).ConfigureTestServices(services =>
        {
            RegisterDatabase(services);
            RegisterUserServices(services);
            RegisterTestServices(services);
            RegisterEmailNotificationServices(services);
            RegisterHangfireServices(services);
            RegisterMfaServices(services);
        });
    }

    private void RegisterEmailNotificationServices(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(Infrastructure.Mailing.AzureCommunicationEmailService));
        services.AddScoped<IEmailNotificationService, InMemoryEmailNotificationService>();
    }

    private void RegisterUserServices(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(IdentityService));
        services.AddScoped<IIdentityService, InMemoryIdentityService>();

        services.RemoveServiceByType(typeof(UsersService));
        services.AddScoped<IUsersService, InMemoryUsersService>();
    }

    private void RegisterTestServices(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(BlobService));
        services.AddSingleton<IBlobService>(_blobService);

        // Remove all IOsPlacesService registrations (added by AddHttpClient)
        var osPlacesDescriptors = services.Where(d => d.ServiceType == typeof(IOsPlacesService)).ToList();
        foreach (var descriptor in osPlacesDescriptors)
        {
            services.Remove(descriptor);
        }

        services.AddScoped<IOsPlacesService, InMemoryOsPlacesService>();

        services.RemoveServiceByType(typeof(InMemoryCache));
        services.AddSingleton<IInMemoryCache>(_fakeCache);

        services.AddDataProtection()
            .SetApplicationName("FamilyHubs.Tests")
            .UseEphemeralDataProtectionProvider();
    }

    private void RegisterDatabase(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(DbContextOptions<ApplicationDbContext>));

        var csb = new NpgsqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            MaxPoolSize = 100,
            Timeout = 30,
            ConnectionIdleLifetime = 30,
            IncludeErrorDetail = true
        };

        var dataSource = new NpgsqlDataSourceBuilder(csb.ConnectionString)
            .EnableDynamicJson()
            .Build();

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(
                    dataSource,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
    }

    private void RegisterHangfireServices(IServiceCollection services)
    {
        // Remove ALL IHostedService registrations to prevent disposal issues during test cleanup
        // This includes Hangfire's BackgroundJobServerHostedService which causes ObjectDisposedException
        var hostedServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .ToList();
        foreach (var descriptor in hostedServiceDescriptors)
        {
            services.Remove(descriptor);
        }

        // Remove any existing Hangfire configuration
        var hangfireDescriptors = services
            .Where(d => d.ServiceType.FullName?.Contains("Hangfire") == true)
            .ToList();
        foreach (var descriptor in hangfireDescriptors)
        {
            services.Remove(descriptor);
        }

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());

        // Do not add Hangfire server in tests - only use in-memory job client
        services.RemoveServiceByType(typeof(Infrastructure.Hangfire.HangfireService));
        services.AddScoped<IHangfireService, InMemoryHangfireService>();
    }

    private void RegisterMfaServices(IServiceCollection services)
    {
        // Modify existing ApplicationSettings to bypass MFA for tests
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ApplicationSettings));
        if (existingDescriptor?.ImplementationInstance is ApplicationSettings existingSettings)
        {
            existingSettings.Mfa.BypassForTesting = true;
            existingSettings.Mfa.SessionExpiryHours = 8;
        }
    }

    public Task InitializeAsync()
    {
        return _dbContainer.StartAsync();
    }

    public new Task DisposeAsync()
    {
        return _dbContainer.DisposeAsync().AsTask();
    }
}

public static class TestFactoryExtensions
{
    public static void RemoveServiceByType(this IServiceCollection services, Type serviceType)
    {
        var descriptor = services.SingleOrDefault(s => s.ServiceType == serviceType);
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}