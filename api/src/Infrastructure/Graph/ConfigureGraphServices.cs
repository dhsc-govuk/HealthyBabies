using Application.Common.Settings;
using Application.Users.Interfaces;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Infrastructure.Graph;

public static class ConfigureGraphServices
{
    public static void AddGraphServices(this IServiceCollection services)
    {
        services.SetupGraphService();
        services.AddScoped<UsersService>();
        services.AddScoped<IUsersService>(provider => provider.GetRequiredService<UsersService>());
    }

    private static void SetupGraphService(this IServiceCollection services)
    {
        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var options = new TokenCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };

        services.AddScoped(provider =>
        {
            var settings = provider.GetRequiredService<ApplicationSettings>();
            var clientSecretCredential = new ClientSecretCredential(
                settings.AzureAd.TenantId,
                settings.AzureAd.ClientId,
                settings.AzureAd.ClientSecret,
                options);
            return new GraphServiceClient(clientSecretCredential, scopes);
        });
    }
}