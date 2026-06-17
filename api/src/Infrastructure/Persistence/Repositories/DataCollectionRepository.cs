using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DataCollectionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DataCollection, DataCollectionId>(dbContext), IDataCollectionRepository, IDataCollectionQueries
{
    public Task<int> Count(CancellationToken cancellationToken = default)
    {
        return Context.DataCollections.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataCollection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.DataCollections
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<DataCollection>> GetByIdAsync(
        DataCollectionId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollections
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<DataCollection>.None;
    }

    public async Task<Option<DataCollection>> GetByIdWithLocalAuthoritiesAsync(
        DataCollectionId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollections
            .AsTracking()
            .Include(x => x.CreatedBy)
            .Include(x => x.LastModifiedBy)
            .Include(x => x.LocalAuthorities)
                .ThenInclude(la => la.LocalAuthority)
            .Include(x => x.FormModuleAssignments)
                .ThenInclude(fm => fm.FormModule)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<DataCollection>.None;
    }

    public async Task<Option<DataCollection>> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollections
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        return entity ?? Option<DataCollection>.None;
    }

    public async Task<Option<DataCollection>> FindDuplicateAsync(
        string targetName,
        DataCollectionId sourceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollections
            .FirstOrDefaultAsync(x => x.Name == targetName && x.Id != sourceId, cancellationToken);
        return entity ?? Option<DataCollection>.None;
    }

    public async Task<IReadOnlyList<DataCollection>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.DataCollections
            .AsNoTracking()
            .Include(x => x.LocalAuthorities)
            .Include(x => x.FormModuleAssignments)
                .ThenInclude(fma => fma.FormModule)
            .Where(x => !x.IsDraft && (x.IsSubmittedByAllLocalAuthorities || x.LocalAuthorities.Any(la => la.LocalAuthorityId == organisationId)))
            .OrderByDescending(x => x.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateFormModuleAssignmentsAsync(
        DataCollectionId dataCollectionId,
        IReadOnlyList<Guid> formModuleIds,
        CancellationToken cancellationToken = default)
    {
        // Skip if no form modules provided
        if (formModuleIds.Count == 0)
        {
            return;
        }

        // Validate that all form module IDs exist in the database
        var formModuleIdList = formModuleIds.ToList();
        var allFormModules = await Context.Set<DataCollectionFormModule>()
            .Select(x => x.Id.Value)
            .ToListAsync(cancellationToken);
        var validFormModuleIds = allFormModules.Where(id => formModuleIdList.Contains(id)).ToList();

        var validFormModuleIdSet = validFormModuleIds.ToHashSet();

        // Get existing assignments directly from DB
        var existingAssignments = await Context.Set<DataCollectionFormModuleAssignment>()
            .Where(x => x.DataCollectionId == dataCollectionId)
            .ToListAsync(cancellationToken);

        var existingFormModuleIds = existingAssignments
            .Select(x => x.FormModuleId.Value)
            .ToHashSet();

        // Find assignments to remove (exist in DB but not in request)
        var assignmentsToRemove = existingAssignments
            .Where(x => !formModuleIds.Contains(x.FormModuleId.Value))
            .ToList();

        if (assignmentsToRemove.Count > 0)
        {
            Context.Set<DataCollectionFormModuleAssignment>().RemoveRange(assignmentsToRemove);
        }

        // Find IDs to add (exist in request but not in DB, and are valid form module IDs)
        var idsToAdd = formModuleIds
            .Where(id => !existingFormModuleIds.Contains(id) && validFormModuleIdSet.Contains(id))
            .ToList();

        if (idsToAdd.Count > 0)
        {
            var newAssignments = idsToAdd.Select(id =>
                DataCollectionFormModuleAssignment.Create(dataCollectionId, new DataCollectionFormModuleId(id)))
                .ToList();

            await Context.Set<DataCollectionFormModuleAssignment>().AddRangeAsync(newAssignments, cancellationToken);
        }

        if (assignmentsToRemove.Count > 0 || idsToAdd.Count > 0)
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}