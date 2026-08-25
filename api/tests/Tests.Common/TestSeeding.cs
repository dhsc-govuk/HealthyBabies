using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Common;

public static class TestSeeding
{
    // Re-runs the seeders the application runs at startup (Program.cs -> app.SeedData()).
    //
    // Deliberately opt-in rather than automatic. The class fixture boots one host per test class, which
    // seeds once; after that, any class that truncates every table wipes the seed. Most classes want
    // exactly that - they truncate and then insert their own rows, and some of those rows collide with
    // the seed (OrganisationAnalyticsControllerTests adds its own "rollover-spend" module, which the
    // DeliveryPlanFormModuleSeeder also writes). Only classes that read seeded rows back call this, from
    // their own InitializeAsync, where it can be awaited.
    //
    // Awaiting matters: seeding from a test constructor would have to block, and under
    // parallelAlgorithm=conservative xUnit installs a MaxConcurrencySyncContext with only
    // maxParallelThreads workers - so every constructor blocked on a continuation queued to that context
    // is a worker that cannot run it.
    public static Task SeedApplicationDataAsync(this IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>().SeedDataAsync();
}