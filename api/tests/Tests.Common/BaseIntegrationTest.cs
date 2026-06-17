using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly ApplicationDbContext Context;
    protected readonly HttpClient Client;
    protected readonly TestServer Server;
    protected readonly IInMemoryCache Cache;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        _scope = factory.Services.CreateScope();

        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = factory.WithWebHostBuilderAuthMock()
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, });

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "TestScheme");

        Server = factory.WithWebHostBuilderAuthMock().Server;
        Cache = _scope.ServiceProvider.GetRequiredService<IInMemoryCache>();
    }

    internal async Task<int> SaveChangesAsync()
    {
        try
        {
            var result = await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            return result;
        }
        catch (Exception ex)
        {
            // Preserve the full exception with inner exceptions
            throw new Exception($"Database SaveChanges failed: {ExceptionHelper.GetDetailedExceptionMessage(ex)}", ex);
        }
    }

    /// <summary>
    /// Enhanced SaveChanges with detailed exception logging.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    protected async Task<int> SaveChangesAsync(string operation = "SaveChanges")
    {
        try
        {
            var result = await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return result;
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(default, ex, $"Database {operation}");
            throw;
        }
    }

    protected async Task ClearAllTablesAsync()
    {
        var tableNames = new List<string>();

        // Step 1: Get table names in a separate connection scope
        await using (var command = Context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = @"
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename <> '__EFMigrationsHistory'";

            if (command.Connection!.State != System.Data.ConnectionState.Open)
            {
                await command.Connection.OpenAsync();
            }

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        // Step 2: Clear data inside a transaction
        await using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            await Context.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");

            foreach (var tableName in tableNames)
            {
                await Context.Database.ExecuteSqlRawAsync($@"TRUNCATE TABLE ""{tableName}"" RESTART IDENTITY CASCADE;");
            }

            await Context.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void Dispose()
    {
        // Dispose HttpClient and DI scope to release DbContext and connections back to the pool
        try
        {
            Client?.Dispose();
        }
        catch
        { /* ignore */
        }

        try
        {
            _scope?.Dispose();
        }
        catch
        { /* ignore */
        }

        GC.SuppressFinalize(this);
    }
}

public class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Role, "admin"), new Claim("userId", "admin") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}