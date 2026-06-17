using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Azure.Storage.Blobs;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static void AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(
                    dataSource,
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

        var blobConnectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrEmpty(blobConnectionString))
        {
            var containerClient = new BlobContainerClient(blobConnectionString, "data-protection-keys");
            containerClient.CreateIfNotExists();

            services.AddDataProtection()
                .SetApplicationName("FamilyHubs")
                .PersistKeysToAzureBlobStorage(containerClient.GetBlobClient("keys.xml"));
        }
        else
        {
            services.AddDataProtection()
                .SetApplicationName("FamilyHubs");
        }

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<ISeederDataService, SeederDataService>();
        services.AddScoped<ISeederDataImporter, SeederDataImporter>();
        services.AddRepositories();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserQueries>(provider => provider.GetRequiredService<UserRepository>());

        services.AddScoped<OrganisationRepository>();
        services.AddScoped<IOrganisationRepository>(provider => provider.GetRequiredService<OrganisationRepository>());
        services.AddScoped<IOrganisationQueries>(provider => provider.GetRequiredService<OrganisationRepository>());

        services.AddScoped<LocationRepository>();
        services.AddScoped<ILocationRepository>(provider => provider.GetRequiredService<LocationRepository>());
        services.AddScoped<ILocationQueries>(provider => provider.GetRequiredService<LocationRepository>());

        services.AddScoped<ServiceRepository>();
        services.AddScoped<IServiceRepository>(provider => provider.GetRequiredService<ServiceRepository>());
        services.AddScoped<IServiceQueries>(provider => provider.GetRequiredService<ServiceRepository>());

        services.AddScoped<ServiceFormQuestionRepository>();
        services.AddScoped<IServiceFormQuestionRepository>(provider => provider.GetRequiredService<ServiceFormQuestionRepository>());
        services.AddScoped<IServiceFormQuestionQueries>(provider => provider.GetRequiredService<ServiceFormQuestionRepository>());

        services.AddScoped<SiteFormQuestionRepository>();
        services.AddScoped<ISiteFormQuestionRepository>(provider => provider.GetRequiredService<SiteFormQuestionRepository>());
        services.AddScoped<ISiteFormQuestionQueries>(provider => provider.GetRequiredService<SiteFormQuestionRepository>());

        services.AddScoped<OrganisationUserRepository>();
        services.AddScoped<IOrganisationUserRepository>(provider => provider.GetRequiredService<OrganisationUserRepository>());
        services.AddScoped<IOrganisationUserQueries>(provider => provider.GetRequiredService<OrganisationUserRepository>());

        services.AddScoped<OrganisationContactRepository>();
        services.AddScoped<IOrganisationContactRepository>(provider => provider.GetRequiredService<OrganisationContactRepository>());
        services.AddScoped<IOrganisationContactQueries>(provider => provider.GetRequiredService<OrganisationContactRepository>());

        services.AddScoped<GlobalDataRepository>();
        services.AddScoped<IGlobalDataRepository>(provider => provider.GetRequiredService<GlobalDataRepository>());
        services.AddScoped<IGlobalDataQueries>(provider => provider.GetRequiredService<GlobalDataRepository>());

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        // Forms
        services.AddScoped<FormSubmissionRepository>();
        services.AddScoped<IFormSubmissionRepository>(provider => provider.GetRequiredService<FormSubmissionRepository>());
        services.AddScoped<IFormSubmissionQueries>(provider => provider.GetRequiredService<FormSubmissionRepository>());

        services.AddScoped<DataSourceRepository>();
        services.AddScoped<IDataSourceRepository>(provider => provider.GetRequiredService<DataSourceRepository>());
        services.AddScoped<IDataSourceQueries>(provider => provider.GetRequiredService<DataSourceRepository>());

        // MFA
        services.AddScoped<IUserMfaRepository, UserMfaRepository>();
        services.AddScoped<IMfaSessionRepository, MfaSessionRepository>();

        // Data Collections
        services.AddScoped<DataCollectionRepository>();
        services.AddScoped<IDataCollectionRepository>(provider => provider.GetRequiredService<DataCollectionRepository>());
        services.AddScoped<IDataCollectionQueries>(provider => provider.GetRequiredService<DataCollectionRepository>());

        services.AddScoped<DataCollectionSubmissionRepository>();
        services.AddScoped<IDataCollectionSubmissionRepository>(provider => provider.GetRequiredService<DataCollectionSubmissionRepository>());
        services.AddScoped<IDataCollectionSubmissionQueries>(provider => provider.GetRequiredService<DataCollectionSubmissionRepository>());
        services.AddScoped<DataCollectionFormModuleRepository>();
        services.AddScoped<IDataCollectionFormModuleRepository>(provider => provider.GetRequiredService<DataCollectionFormModuleRepository>());
        services.AddScoped<IDataCollectionFormModuleQueries>(provider => provider.GetRequiredService<DataCollectionFormModuleRepository>());

        services.AddScoped<FormFieldRepository>();
        services.AddScoped<IFormFieldRepository>(provider => provider.GetRequiredService<FormFieldRepository>());
        services.AddScoped<IFormFieldQueries>(provider => provider.GetRequiredService<FormFieldRepository>());

        // Service Categories
        services.AddScoped<ServiceCategoryRepository>();
        services.AddScoped<IServiceCategoryRepository>(provider => provider.GetRequiredService<ServiceCategoryRepository>());
        services.AddScoped<IServiceCategoryQueries>(provider => provider.GetRequiredService<ServiceCategoryRepository>());

        services.AddScoped<ServiceCategoryFormQuestionRepository>();
        services.AddScoped<IServiceCategoryFormQuestionQueries>(provider => provider.GetRequiredService<ServiceCategoryFormQuestionRepository>());
    }
}