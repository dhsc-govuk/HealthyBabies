using Application.Common.Interfaces;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Infrastructure.Hangfire;

public static class ConfigureHangfire
{
    public static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireConnection = configuration.GetConnectionString("DefaultConnection")!;

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
                options => options.UseNpgsqlConnection(hangfireConnection),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire_schema",
                    PrepareSchemaIfNecessary = true
                }));

        services.AddHangfireServer();

        services.AddScoped<IHangfireService, HangfireService>();
    }
}