using Application.Common.Dtos;
using Application.Common.Interfaces;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Tests.Common.Services;

public class InMemoryOsPlacesService : IOsPlacesService
{
    public Task<Either<Exception, IReadOnlyList<OsPlacesAddressDto>>> SearchByPostcodeAsync(
        string postcode,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OsPlacesAddressDto> addresses = new List<OsPlacesAddressDto>
        {
            new(
                Uprn: "100023336956",
                Address: "1, TEST STREET, LONDON, SW1A 1AA",
                BuildingName: null,
                BuildingNumber: "1",
                ThoroughfareName: "TEST STREET",
                DependentLocality: null,
                PostTown: "LONDON",
                Postcode: postcode.ToUpperInvariant(),
                OrganisationName: null),
            new(
                Uprn: "100023336957",
                Address: "2, TEST STREET, LONDON, SW1A 1AA",
                BuildingName: null,
                BuildingNumber: "2",
                ThoroughfareName: "TEST STREET",
                DependentLocality: null,
                PostTown: "LONDON",
                Postcode: postcode.ToUpperInvariant(),
                OrganisationName: null)
        };

        return Task.FromResult(Right<Exception, IReadOnlyList<OsPlacesAddressDto>>(addresses));
    }
}