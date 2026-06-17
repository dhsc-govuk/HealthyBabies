using Application.Common.Permissions;
using Domain.Locations;
using Domain.Organisations;
using LanguageExt;
using Location = Domain.Locations.Location;

namespace Application.Common.Interfaces.Repositories;

public interface ILocationRepository
{
    Task<Option<Location>> GetById(LocationId id, Permission permission, CancellationToken cancellationToken = default);
    Task<Option<Location>> GetByIdForUpdate(LocationId id, Permission permission, CancellationToken cancellationToken = default);
    Task<Option<Location>> FindByNameAsync(string name, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Option<Location>> FindByReferenceNumberAsync(string referenceNumber, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Option<Location>> FindDuplicateAsync(string targetName, OrganisationId organisationId, LocationId sourceId, CancellationToken cancellationToken = default);
    Task<Option<Location>> FindDuplicateByReferenceNumberAsync(string referenceNumber, OrganisationId organisationId, LocationId sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> FindByNamesOrUprnAsync(IReadOnlyList<string> namesOrUprns, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Location> AddAsync(Location entity, CancellationToken cancellationToken = default);
    Task<Location> UpdateAsync(Location entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Location entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(
      IReadOnlyList<Location> entities,
      CancellationToken cancellationToken = default,
      bool saveChanges = true);
}