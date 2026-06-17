using Application.Common.Permissions;
using Domain.Locations;
using Domain.Organisations;
using LanguageExt;
using Location = Domain.Locations.Location;

namespace Application.Common.Interfaces.Queries;

public interface ILocationQueries
{
    Task<IReadOnlyList<Location>> GetGlobal(CancellationToken cancellationToken);

    Task<IReadOnlyList<Location>> GetAll(
        OrganisationId id,
        Permission permission,
        CancellationToken cancellationToken = default);

    Task<Option<Location>> GetById(
        LocationId id,
        Permission permission,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> GetAllForExport(CancellationToken cancellationToken = default);

    Task<int> Count(
        Option<OrganisationId> organisationId,
        Permission permission,
        CancellationToken cancellationToken = default);
}