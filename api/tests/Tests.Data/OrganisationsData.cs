using Domain.Organisations;

namespace Tests.Data;

public static class OrganisationsData
{
    public static readonly Organisation MainOrganisation = Organisation.New(
        id: OrganisationId.New(),
        name: "Main",
        oNSCode: "E00000001",
        isActive: true);

    public static readonly Organisation SecondOrganisation = Organisation.New(
        id: OrganisationId.New(),
        name: "Second",
        oNSCode: "E00000002",
        isActive: true);
}