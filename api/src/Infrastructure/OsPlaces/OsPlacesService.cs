using System.Net.Http.Json;
using Application.Common.Dtos;
using Application.Common.Interfaces;
using Application.Common.Settings;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Infrastructure.OsPlaces;

public class OsPlacesService(HttpClient httpClient, ApplicationSettings settings) : IOsPlacesService
{
    public async Task<Either<Exception, IReadOnlyList<OsPlacesAddressDto>>> SearchByPostcodeAsync(
        string postcode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedPostcode = Uri.EscapeDataString(postcode);
            var url = $"{settings.OsPlaces.BaseUrl}/postcode?postcode={encodedPostcode}&key={settings.OsPlaces.ApiKey}";

            var response = await httpClient.GetFromJsonAsync<OsPlacesApiResponse>(url, cancellationToken);

            if (response?.Results == null)
            {
                return Right<Exception, IReadOnlyList<OsPlacesAddressDto>>(new List<OsPlacesAddressDto>());
            }

            var addresses = response.Results
                .Where(r => r.Dpa != null)
                .Select(r => new OsPlacesAddressDto(
                    Uprn: r.Dpa!.Uprn,
                    Address: r.Dpa.Address,
                    BuildingName: r.Dpa.BuildingName,
                    BuildingNumber: r.Dpa.BuildingNumber,
                    ThoroughfareName: r.Dpa.ThoroughfareName,
                    DependentLocality: r.Dpa.DependentLocality,
                    PostTown: r.Dpa.PostTown,
                    Postcode: r.Dpa.Postcode,
                    OrganisationName: r.Dpa.OrganisationName))
                .ToList();

            return Right<Exception, IReadOnlyList<OsPlacesAddressDto>>(addresses);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}