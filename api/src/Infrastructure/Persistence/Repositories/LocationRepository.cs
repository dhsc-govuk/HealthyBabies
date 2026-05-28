using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Domain.Locations;
using Domain.Organisations;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LocationRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Location, LocationId>(dbContext), ILocationRepository, ILocationQueries
{
    public async Task<IReadOnlyList<Location>> GetGlobal(CancellationToken cancellationToken)
    {
        return await Context.Locations
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetAllForExport(CancellationToken cancellationToken = default)
    {
        return await Context.Locations
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers.OrderBy(a => a.DisplayOrder))
            .OrderBy(x => x.Organisation!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetAll(
        OrganisationId id,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await GetQuery(permission.UserRole, permission.OrganisationId, permission.LocationId)
            .Include(x => x.Answers)
            .Where(x => x.OrganisationId == id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<Location>> GetById(LocationId id, Permission permission, CancellationToken cancellationToken)
    {
        var entity = await GetLocationByIdQuery(id, permission)
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> GetByIdForUpdate(LocationId id, Permission permission, CancellationToken cancellationToken)
    {
        var entity = await GetLocationByIdQueryForUpdate(id, permission)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> GetById(
        LocationId id,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var entity = await Context.Locations
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganisationId == organisationId, cancellationToken);

        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> FindByNameAsync(string name, OrganisationId organisationId, CancellationToken cancellationToken)
    {
        var entity = await Context.Locations
            .FirstOrDefaultAsync(x => x.Name == name && x.OrganisationId == organisationId, cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> FindByReferenceNumberAsync(string referenceNumber, OrganisationId organisationId, CancellationToken cancellationToken)
    {
        var entity = await Context.Locations
            .FirstOrDefaultAsync(x => x.ReferenceNumber == referenceNumber && x.OrganisationId == organisationId, cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> FindDuplicateAsync(string targetName, OrganisationId organisationId, LocationId sourceId, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Locations
            .FirstOrDefaultAsync(x => x.Name == targetName && x.OrganisationId == organisationId && x.Id != sourceId, cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<Option<Location>> FindDuplicateByReferenceNumberAsync(string referenceNumber, OrganisationId organisationId, LocationId sourceId, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Locations
            .FirstOrDefaultAsync(x => x.ReferenceNumber == referenceNumber && x.OrganisationId == organisationId && x.Id != sourceId, cancellationToken);
        return entity ?? Option<Location>.None;
    }

    public async Task<IReadOnlyList<Location>> FindByNamesOrUprnAsync(
        IReadOnlyList<string> namesOrUprns,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedValues = namesOrUprns.Select(n => n.ToLowerInvariant()).ToList();
        return await Context.Locations
            .Include(x => x.Answers)
            .Where(x => x.OrganisationId == organisationId &&
                        (normalizedValues.Contains(x.Name.ToLower()) ||
                         normalizedValues.Contains(x.ReferenceNumber.ToLower())))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> Count(
        Option<OrganisationId> organisationId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await organisationId.MatchAsync(
            orgId => GetQuery(permission.UserRole, permission.OrganisationId, permission.LocationId)
                .CountAsync(x => x.OrganisationId == orgId, cancellationToken),
            () => GetQuery(permission.UserRole, permission.OrganisationId, permission.LocationId)
                .CountAsync(cancellationToken));
    }

    private IQueryable<Location> GetLocationByIdQuery(LocationId id, Permission permission)
    {
        return GetQuery(permission.UserRole, permission.OrganisationId, permission.LocationId).Where(x => x.Id == id);
    }

    private IQueryable<Location> GetLocationByIdQueryForUpdate(LocationId id, Permission permission)
    {
        return GetQueryForUpdate(permission.UserRole, permission.OrganisationId).Where(x => x.Id == id);
    }

    private IQueryable<Location> GetQuery(UserRole userRole, Option<OrganisationId> organisationId, Option<LocationId> locationId)
    {
        if (userRole.Equals(UserRole.Admin))
        {
            return Context.Locations.AsNoTracking();
        }

        if (userRole.Equals(UserRole.OrganisationAdmin) && organisationId.IsSome)
        {
            return organisationId.Match(
                orgId => Context.Locations.AsNoTracking().Where(x => x.OrganisationId == orgId),
                Array.Empty<Location>().AsQueryable());
        }

        return Array.Empty<Location>().AsQueryable();
    }

    private IQueryable<Location> GetQueryForUpdate(UserRole userRole, Option<OrganisationId> organisationId)
    {
        if (userRole.Equals(UserRole.Admin))
        {
            return Context.Locations;
        }

        if (userRole.Equals(UserRole.OrganisationAdmin) && organisationId.IsSome)
        {
            return organisationId.Match(
                orgId => Context.Locations.Where(x => x.OrganisationId == orgId),
                Array.Empty<Location>().AsQueryable());
        }

        return Array.Empty<Location>().AsQueryable();
    }
}