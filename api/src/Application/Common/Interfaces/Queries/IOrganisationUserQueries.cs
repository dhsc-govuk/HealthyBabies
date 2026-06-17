using Application.Common.Permissions;
using Domain.Organisations;
using Domain.OrganisationUsers;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IOrganisationUserQueries
{
    Task<int> Count(Option<OrganisationId> id, Permission permission, CancellationToken cancellationToken);
    Task<Option<OrganisationUser>> GetOrganisationUserById(OrganisationUserId id, Permission permission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganisationUser>> GetUsers(OrganisationId organisationId, Permission permission, CancellationToken cancellationToken);
}