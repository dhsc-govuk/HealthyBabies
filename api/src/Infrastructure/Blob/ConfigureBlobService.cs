using Application.Common.Interfaces;
using Application.Common.Settings;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Blob;

public static class ConfigureBlobService
{
    public static void AddBlobServices(this IServiceCollection services)
    {
        services.SetupBlobService();
        services.AddScoped<BlobService>();
        services.AddScoped<IBlobService>(provider => provider.GetRequiredService<BlobService>());
    }

    private static void SetupBlobService(this IServiceCollection services)
    {
        services.AddScoped(provider =>
        {
            var settings = provider.GetRequiredService<ApplicationSettings>();

            return new BlobServiceClient(settings.AzureBlob.ConnectionString);
        });
    }
}