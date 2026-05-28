using System.Security.Claims;
using Api.Services.Abstract;
using Api.Services.Implementation;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Users.Dtos;
using Domain.Users;
using FluentValidation;
using FluentValidation.AspNetCore;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web;

namespace Api.Modules;

public class ApplicationUserClaimsTransformation(
    IInMemoryCache cache,
    IUserProfileRepository userProfileRepository)
    : IClaimsTransformation
{
    private async Task<ClaimsPrincipal> GetUserProfile(ClaimsPrincipal principal, SubId subId)
    {
        var cacheKey = $"user::{subId.Value}";
        var cachedUser = cache.GetItem<Profile>(cacheKey);

        cachedUser.IfSome(u => SetUserClaims(principal, u));
        if (cachedUser.IsSome)
        {
            return principal;
        }

        var user = await userProfileRepository.GetUserProfile(subId);
        return await user.MatchAsync(
           u =>
           {
               var userProfile = new Profile(
                  u.Id!.Value,
                  u.Name!.FirstName,
                  u.Name.LastName,
                  u.Email!,
                  u.IsActive,
                  u.Role!.ToString(),
                  u.OrganisationId.IsSome ? u.OrganisationId.ValueUnsafe().Value : null,
                  u.LocationId.IsSome ? u.LocationId.ValueUnsafe().Value : null,
                  u.OrganisationName.IsSome ? u.OrganisationName.ValueUnsafe() : null);

               return Task.FromResult(UpdateCache(userProfile, cacheKey, principal));
           },
           () => principal);
    }

    private ClaimsPrincipal UpdateCache(Profile profile, string cacheKey, ClaimsPrincipal principal)
    {
        cache.PutItem(cacheKey, profile, TimeSpan.FromMinutes(5));
        SetUserClaims(principal, profile);

        return principal;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var subIdResult = GetSubId(principal);
        return subIdResult.MatchAsync(
            subId => GetUserProfile(principal, subId),
            () => principal);
    }

    private Option<SubId> GetSubId(ClaimsPrincipal principal)
    {
        var subId = principal.FindFirstValue(ClaimConstants.ObjectId);
        return string.IsNullOrEmpty(subId)
            ? Option<SubId>.None
            : new SubId(new Guid(subId));
    }

    private void SetUserClaims(ClaimsPrincipal principal, Profile profile)
    {
        ClaimsIdentity claimsIdentity = new();
        if (!principal.HasClaim(claim => claim.Type == ClaimTypes.Role))
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, profile.Role!.ToString()));
        }

        if (!principal.HasClaim(claim => claim.Type == "userId"))
        {
            claimsIdentity.AddClaim(new Claim("userId", profile.Id.ToString()));
        }

        if (!principal.HasClaim(claim => claim.Type == "organisationId"))
        {
            claimsIdentity.AddClaim(new Claim("organisationId", profile.OrganisationId.ToString() ?? string.Empty));
        }

        if (!principal.HasClaim(claim => claim.Type == "locationId"))
        {
            claimsIdentity.AddClaim(new Claim("locationId", profile.LocationId.ToString() ?? string.Empty));
        }

        if (!principal.HasClaim(claim => claim.Type == ClaimTypes.Email) && !string.IsNullOrEmpty(profile.Email))
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, profile.Email));
        }

        principal.AddIdentity(claimsIdentity);
    }
}

internal static class SetupModule
{
    internal static void SetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.SetupControllers();
        services.AddRequestValidators();
        services.AddMemoryCache();
        services.SetupAuthentication(configuration);
        services.SetupCors(configuration);
        services.SetupConfiguration(configuration);
        services.AddSignalR(o =>
        {
            o.EnableDetailedErrors = true;
        });
        services.SetupControllerServices();
    }

    private static void SetupAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMicrosoftIdentityWebApiAuthentication(configuration, "AzureAd");
        services.AddAuthorization();
        services.AddTransient<IClaimsTransformation, ApplicationUserClaimsTransformation>();
        services.AddHttpContextAccessor();
    }

    private static void SetupCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(origin => true)
                    .AllowCredentials()));
    }

    private static void SetupControllers(this IServiceCollection services)
    {
        services.AddControllers();
    }

    private static void AddRequestValidators(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<Program>();
    }

    private static void SetupConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.Get<ApplicationSettings>();
        if (settings != null)
        {
            services.AddSingleton(settings);
        }
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), @"MyStaticFiles")),
            RequestPath = new PathString("/App_Data")
        });
    }

    private static void SetupControllerServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminHomeControllerService, AdminHomeControllerService>();
        services.AddScoped<IAdminUsersControllerService, AdminUsersControllerService>();
        services.AddScoped<ILocationsControllerService, LocationsControllerService>();
        services.AddScoped<IOrganisationManagerHomeControllerService, OrganisationManagerHomeControllerService>();
        services.AddScoped<IOrganisationsControllerService, OrganisationsControllerService>();
        services.AddScoped<IOrganisationContactsControllerService, OrganisationContactsControllerService>();
        services.AddScoped<IOrganisationUsersControllerService, OrganisationUsersControllerService>();
        services.AddScoped<IProfileControllerService, ProfileControllerService>();
        services.AddScoped<IGlobalDataControllerService, GlobalDataControllerService>();
        services.AddScoped<IServicesControllerService, ServicesControllerService>();
        services.AddScoped<IServiceCategoriesControllerService, ServiceCategoriesControllerService>();
        services.AddScoped<IDataCollectionsControllerService, DataCollectionsControllerService>();
        services.AddScoped<ISubmissionsControllerService, SubmissionsControllerService>();
        services.AddScoped<ISubmissionExportService, SubmissionExportService>();
        services.AddScoped<ICoreDataExportService, CoreDataExportService>();
        services.AddScoped<IBulkUploadTemplateService, BulkUploadTemplateService>();
        services.AddScoped<IServicesBulkUploadTemplateService, ServicesBulkUploadTemplateService>();
        services.AddScoped<IServiceCategoriesBulkUploadTemplateService, ServiceCategoriesBulkUploadTemplateService>();
        services.AddScoped<ISubmissionsBulkUploadService, SubmissionsBulkUploadService>();

        // Bulk upload file parsers
        services.AddScoped<IBulkUploadFileParser, Application.Common.CsvParser>();
        services.AddScoped<IBulkUploadFileParser, Application.Common.ExcelParser>();
    }
}