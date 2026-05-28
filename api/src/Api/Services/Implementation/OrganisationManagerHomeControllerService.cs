using Api.Services.Abstract;

using Application.Common.Interfaces.Queries;

using Application.Common.Permissions;

using Application.Organisations.Dtos;

using Domain.Organisations;

using LanguageExt;

namespace Api.Services.Implementation;

public class OrganisationManagerHomeControllerService(

    IOrganisationUserQueries organisationUserQueries,

    ILocationQueries locationQueries,

    IOrganisationContactQueries organisationContactQueries) : IOrganisationManagerHomeControllerService
{
    public async Task<OrganisationAdminHomeDto> GetTotals(Permission permission, CancellationToken cancellationToken)
    {
        var contactsCount = await permission.OrganisationId.MatchAsync(

            async orgId => await organisationContactQueries.CountByOrganisationIdAsync(orgId, cancellationToken),

            () => 0);

        return new OrganisationAdminHomeDto(

            Admins: await organisationUserQueries.Count(Option<OrganisationId>.None, permission, cancellationToken),

            Locations: await locationQueries.Count(Option<OrganisationId>.None, permission, cancellationToken),

            Contacts: contactsCount);
    }
}