using Domain.Organisations;

namespace Tests.Data;

public static class OrganisationContactsData
{
    public static OrganisationContact MainContact(OrganisationId organisationId) => OrganisationContact.New(
        id: OrganisationContactId.New(),
        organisationId: organisationId,
        name: "Main Contact",
        email: "main@example",
        role: "Manager");

    public static OrganisationContact SecondContact(OrganisationId organisationId) => OrganisationContact.New(
        id: OrganisationContactId.New(),
        organisationId: organisationId,
        name: "Second Contact",
        email: "second@example.com",
        role: "Sales Manager");
}