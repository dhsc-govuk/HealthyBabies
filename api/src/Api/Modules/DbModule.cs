using Infrastructure.Persistence;

namespace Api.Modules;

internal static class DbModule
{
    internal static async Task InitializeDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initializer.InitialiseAsync();
    }

    internal static async Task SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initializer.SeedDataAsync();
    }
}