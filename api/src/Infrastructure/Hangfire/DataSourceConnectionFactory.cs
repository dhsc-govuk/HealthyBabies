using Hangfire.PostgreSql;
using Npgsql;

namespace Infrastructure.Hangfire;

/// <summary>
/// Creates Hangfire's Postgres connections from the shared <see cref="NpgsqlDataSource"/> instead
/// of a raw connection string, so Hangfire picks up the same Entra ID token-refresh behaviour
/// (when configured) as EF Core's connection - see PostgresDataSourceFactory.
/// </summary>
internal sealed class DataSourceConnectionFactory(NpgsqlDataSource dataSource) : IConnectionFactory
{
    public NpgsqlConnection GetOrCreateConnection() => dataSource.CreateConnection();
}