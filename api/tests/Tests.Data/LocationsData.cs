using Domain.Locations;
using Domain.Organisations;

namespace Tests.Data;

public static class LocationsData
{
    public static Location MainLocation(OrganisationId organisationId) => CreateLocation(organisationId, "Main");

    public static Location SecondLocation(OrganisationId organisationId) => CreateLocation(organisationId, "Second Location");

    private static Location CreateLocation(
    OrganisationId organisationId,
    string name)
    {
        return Location.New(
            id: LocationId.New(),
            organisationId: organisationId,
            name: name,
            postCode: "AB12 3CD",
            referenceNumber: Guid.NewGuid().ToString(),
            isActive: true);
    }
}