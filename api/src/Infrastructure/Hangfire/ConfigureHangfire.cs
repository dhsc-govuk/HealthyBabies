using Application.Common.Interfaces;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Infrastructure.Hangfire;

public static class ConfigureHangfire
{
    public static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSource = PostgresDataSourceFactory.Build(
            configuration.GetConnectionString("DefaultConnection")!,
            configuration["Database:ManagedIdentityClientId"]);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSerializerSettings(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            })
            .UsePostgreSqlStorage(
                options => options.UseConnectionFactory(new DataSourceConnectionFactory(dataSource)),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire_schema",
                    PrepareSchemaIfNecessary = true
                }));

        services.AddHangfireServer();

        services.AddScoped<IHangfireService, HangfireService>();
    }
}