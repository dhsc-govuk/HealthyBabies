using Application.Common.Permissions;
using Domain.Organisations;
using Domain.Services;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IServiceRepository
{
    Task<Option<Service>> GetById(Permission permission, ServiceId id, CancellationToken cancellationToken = default);
    Task<Option<Service>> GetByIdForUpdate(Permission permission, ServiceId id, CancellationToken cancellationToken = default);
    Task<Option<Service>> FindByNameAsync(string name, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Option<Service>> FindDuplicateAsync(string targetName, OrganisationId organisationId, ServiceId sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> FindByNamesAsync(IReadOnlyList<string> names, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Service> AddAsync(Service entity, CancellationToken cancellationToken = default);
    Task<Service> UpdateAsync(Service entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Service entity, CancellationToken cancellationToken = default);
}