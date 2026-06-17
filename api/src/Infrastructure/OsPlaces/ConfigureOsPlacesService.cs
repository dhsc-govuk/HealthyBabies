using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.OsPlaces;

public static class ConfigureOsPlacesService
{
    public static void AddOsPlacesServices(this IServiceCollection services)
    {
        services.AddHttpClient<IOsPlacesService, OsPlacesService>();
    }
}