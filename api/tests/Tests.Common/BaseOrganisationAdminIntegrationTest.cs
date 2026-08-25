using System.Net.Http.Headers;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Common;

public abstract class BaseOrganisationAdminIntegrationTest : IClassFixture<OrganisationAdminIntegrationTestWebFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly ApplicationDbContext Context;
    protected readonly HttpClient Client;
    protected readonly TestServer Server;
    protected readonly IInMemoryCache Cache;

    protected BaseOrganisationAdminIntegrationTest(OrganisationAdminIntegrationTestWebFactory factory)
    {
        _scope = factory.Services.CreateScope();

        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Deliberately NOT factory.WithWebHostBuilder(...): this constructor runs once per test, and each
        // WithWebHostBuilder call mints a new WebApplicationFactory whose host runs the real entry point,
        // re-running InitializeDb and SeedData against the same container - and the derived factories are
        // held alive by the parent until the class fixture disposes, so none of it is reclaimed mid-run.
        // The TestScheme registration that used to live here now sits in
        // OrganisationAdminIntegrationTestWebFactory.ConfigureWebHost, so the class fixture boots exactly
        // one host and both Client and Server below just reuse it.
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, });

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "TestScheme");

        Server = factory.Server;
        Cache = _scope.ServiceProvider.GetRequiredService<IInMemoryCache>();
    }

    // Restores the app's startup seed for classes that truncate every table and then read seeded rows
    // back. Call it from the class's own InitializeAsync; see TestSeeding for why it is opt-in.
    protected async Task SeedApplicationDataAsync()
    {
        await _scope.SeedApplicationDataAsync();
        Context.ChangeTracker.Clear();
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
            throw new Exception($"Database SaveChanges failed: {ExceptionHelper.GetDetailedExceptionMessage(ex)}", ex);
        }
    }

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