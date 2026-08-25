using Domain.DataCollections;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IDataCollectionSubmissionVersionRepository
{
    Task<int> GetCountByDataCollectionSubmissionIdAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        CancellationToken cancellationToken = default);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<DataCollectionSubmissionVersion>> GetByDataCollectionSubmissionIdAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        CancellationToken cancellationToken = default);

    Task<Option<DataCollectionSubmissionVersion>> GetByDataCollectionSubmissionIdAndVersionNumberAsync(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    Task<DataCollectionSubmissionVersion> AddAsync(
        DataCollectionSubmissionVersion entity,
        CancellationToken cancellationToken = default,
        bool saveChanges = true);
}