using Application.Common.Permissions;
using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IOrganisationRepository
{
    Task<Option<Organisation>> GetOrganisationById(OrganisationId id, CancellationToken cancellationToken = default);
    Task<Option<Organisation>> GetOrganisationById(OrganisationId id, Permission permission, CancellationToken cancellationToken = default);
    Task<Option<Organisation>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Option<Organisation>> FindDuplicateAsync(string targetName, OrganisationId sourceId, CancellationToken cancellationToken = default);
    Task<Organisation> AddAsync(Organisation entity, CancellationToken cancellationToken = default);
    Task<Organisation> UpdateAsync(Organisation entity, CancellationToken cancellationToken = default);
}