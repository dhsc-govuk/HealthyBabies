using Application.Common.Permissions;
using Domain.OrganisationUsers;
using Domain.Users;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IOrganisationUserRepository
{
    Task<Option<OrganisationUser>> GetOrganisationUserById(OrganisationUserId id, Permission permission, CancellationToken cancellationToken = default);
    Task<Option<OrganisationUser>> FindByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<OrganisationUser> AddAsync(OrganisationUser entity, CancellationToken cancellationToken = default);
    Task<OrganisationUser> UpdateAsync(OrganisationUser entity, CancellationToken cancellationToken = default);
}