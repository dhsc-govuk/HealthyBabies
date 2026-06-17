using Application.Common.Settings;
using Domain.Users;
using Domain.ValueObjects;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    ApplicationDbContext dbContextFactory,
    ApplicationSettings settings,
    ISeederDataImporter seederDataImporter)
{
    public async Task InitialiseAsync()
    {
        try
        {
            await dbContextFactory.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedDataAsync()
    {
        await SeedAdminUser();
        await SeedServiceFormQuestions();
    }

    private async Task SeedServiceFormQuestions()
    {
        try
        {
            // Try to import from exported JSON data first (for prod deployment)
            if (settings.Seeder.UseExportedData)
            {
                var result = await seederDataImporter.ImportManuallyAsync(settings.Seeder.FlushExistingData);
                if (result.Success)
                {
                    logger.LogInformation("Seeder data imported from exported JSON file");
                    return;
                }
            }

            // Fall back to code-based seeders
            logger.LogInformation("Using code-based seeders");
            await ServiceFormQuestionSeeder.SeedAsync(dbContextFactory);
            await SiteFormQuestionSeeder.SeedAsync(dbContextFactory);
            await DataCollectionFormModuleSeeder.SeedAsync(dbContextFactory);
            await WiderServiceCategorySeeder.SeedAsync(dbContextFactory);
            await ServiceCategoryFormQuestionSeeder.SeedAsync(dbContextFactory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding service form questions.");
            throw;
        }
    }

    private async Task SeedAdminUser()
    {
        try
        {
            var userEmail = settings.DefaultAdminUser.Email;
            var usersExist = await dbContextFactory.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (usersExist == null)
            {
                var email = settings.DefaultAdminUser.Email;
                var subId = new SubId(Guid.Parse(settings.DefaultAdminUser.SubId));
                var user = User.New(
                    new UserId(Guid.NewGuid()),
                    new Name("familyhubs", "administrator"),
                    email,
                    subId,
                    true,
                    UserRole.Admin);
                dbContextFactory.Users.Add(user);
                await dbContextFactory.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding default admin user.");
            throw;
        }
    }
}