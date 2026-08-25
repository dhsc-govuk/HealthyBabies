using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DataCollectionSubmissionVersionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DataCollectionSubmissionVersion, DataCollectionSubmissionVersionId>(dbContext),
      IDataCollectionSubmissionVersionRepository
{
    public async Task<int> GetCountByDataCollectionSubmissionIdAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionSubmissionVersions
            .AsNoTracking()
            .CountAsync(x => x.DataCollectionSubmissionId == dataCollectionSubmissionId, cancellationToken);
    }

    public async Task<IReadOnlyList<DataCollectionSubmissionVersion>> GetByDataCollectionSubmissionIdAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionSubmissionVersions
            .AsNoTracking()
            .Include(x => x.SubmittedBy)
            .Where(x => x.DataCollectionSubmissionId == dataCollectionSubmissionId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<DataCollectionSubmissionVersion>> GetByDataCollectionSubmissionIdAndVersionNumberAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionSubmissionVersions
            .AsNoTracking()
            .Include(x => x.SubmittedBy)
            .FirstOrDefaultAsync(
                x => x.DataCollectionSubmissionId == dataCollectionSubmissionId && x.VersionNumber == versionNumber,
                cancellationToken);
        return entity ?? Option<DataCollectionSubmissionVersion>.None;
    }
}