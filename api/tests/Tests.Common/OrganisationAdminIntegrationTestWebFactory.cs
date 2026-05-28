using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Users.Interfaces;
using Hangfire;
using Infrastructure.Blob;
using Infrastructure.Caching;
using Infrastructure.Graph;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.RequestStaging;
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

public class OrganisationAdminIntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly FakeMemoryCache _fakeCache;
    private readonly InMemoryRequestStagingService _requestStaging;
    private readonly InMemoryBlobService _blobService;

    public OrganisationAdminIntegrationTestWebFactory()
    {
        _fakeCache = new FakeMemoryCache();
        _requestStaging = new InMemoryRequestStagingService();
        _blobService = new InMemoryBlobService();
    }

    public FakeMemoryCache FakeCache => _fakeCache;

    public InMemoryRequestStagingService RequestStaging => _requestStaging;

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
        services.RemoveServiceByType(typeof(Infrastructure.Mailing.EmailNotificationService));
        services.AddScoped<IEmailNotificationService, InMemoryEmailNotificationService>();
    }

    private void RegisterUserServices(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(IdentityService));
        services.AddScoped<IIdentityService, InMemoryOrganisationAdminIdentityService>();

        services.RemoveServiceByType(typeof(UsersService));
        services.AddScoped<IUsersService, InMemoryUsersService>();
    }

    private void RegisterTestServices(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(BlobService));
        services.AddSingleton<IBlobService>(_blobService);

        services.RemoveServiceByType(typeof(RequestStagingService));
        services.AddSingleton<IRequestStagingService>(_requestStaging);

        services.RemoveServiceByType(typeof(InMemoryCache));
        services.AddSingleton<IInMemoryCache>(_fakeCache);
    }

    private void RegisterDatabase(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(DbContextOptions<ApplicationDbContext>));

        var csb = new NpgsqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            MaxPoolSize = 100,
            Timeout = 30,
            ConnectionIdleLifetime = 30
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
        // Remove ALL IHostedService registrations to prevent disposal issues during test cleanup.
        // Hangfire's BackgroundJobServerHostedService otherwise races with WebApplicationFactory
        // disposal and throws ObjectDisposedException on BackgroundProcessingServer.
        var hostedServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .ToList();
        foreach (var descriptor in hostedServiceDescriptors)
        {
            services.Remove(descriptor);
        }

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

    public WebApplicationFactory<Program> WithWebHostBuilderAuthMock()
    {
        return this.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, OrganisationAdminTestAuthHandler>(
                        "TestScheme", _ => { });
            });
        });
    }
}