using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DataCollectionSubmissionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DataCollectionSubmission, DataCollectionSubmissionId>(dbContext),
      IDataCollectionSubmissionRepository,
      IDataCollectionSubmissionQueries
{
    public async Task<Option<DataCollectionSubmission>> GetByIdAsync(
        DataCollectionSubmissionId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionSubmissions
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<DataCollectionSubmission>.None;
    }

    public async Task<Option<DataCollectionSubmission>> GetByDataCollectionAndOrganisationAsync(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionSubmissions
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.DataCollectionId == dataCollectionId && x.OrganisationId == organisationId,
                cancellationToken);
        return entity ?? Option<DataCollectionSubmission>.None;
    }

    public async Task<Option<DataCollectionSubmission>> GetByDataCollectionAndOrganisationWithSectionsAsync(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionSubmissions
            .AsTracking()
            .Include(x => x.DataCollection)
                .ThenInclude(dc => dc!.FormModuleAssignments)
                    .ThenInclude(fma => fma.FormModule)
            .FirstOrDefaultAsync(
                x => x.DataCollectionId == dataCollectionId && x.OrganisationId == organisationId,
                cancellationToken);
        return entity ?? Option<DataCollectionSubmission>.None;
    }

    public async Task<IReadOnlyList<DataCollectionSubmission>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionSubmissions
            .AsNoTracking()
            .Include(x => x.DataCollection)
            .Where(x => x.OrganisationId == organisationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataCollectionSubmission>> GetByDataCollectionIdAsync(
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionSubmissions
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.SubmittedBy)
            .Where(x => x.DataCollectionId == dataCollectionId)
            .OrderBy(x => x.Organisation!.Name)
            .ToListAsync(cancellationToken);
    }
}