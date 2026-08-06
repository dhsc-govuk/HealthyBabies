using Azure.Core;
using Azure.Identity;
using Npgsql;

namespace Infrastructure.Persistence;

/// <summary>
/// Builds the shared Postgres data source used by both EF Core and Hangfire. When a Managed
/// Identity client ID is configured, connects via an Entra ID access token (refreshed
/// periodically) instead of the password embedded in the connection string; otherwise falls back
/// to the plain connection string exactly as before.
/// </summary>
public static class PostgresDataSourceFactory
{
    private const string PostgresAadScope = "https://ossrdbms-aad.database.windows.net/.default";

    public static NpgsqlDataSource Build(string connectionString, string? managedIdentityClientId)
    {
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            // Npgsql refuses to register a password provider when the connection string still
            // carries a static password - strip it so the token provider is the only source.
            var csb = new NpgsqlConnectionStringBuilder(connectionString) { Password = null };
            connectionString = csb.ConnectionString;
        }

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.EnableDynamicJson();

        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            var credential = new ManagedIdentityCredential(managedIdentityClientId);
            var tokenContext = new TokenRequestContext([PostgresAadScope]);

            builder.UsePeriodicPasswordProvider(
                async (_, ct) => (await credential.GetTokenAsync(tokenContext, ct)).Token,
                successRefreshInterval: TimeSpan.FromMinutes(45),
                failureRefreshInterval: TimeSpan.FromSeconds(10));
        }

        return builder.Build();
    }
}