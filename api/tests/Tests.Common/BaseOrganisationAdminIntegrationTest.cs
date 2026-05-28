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