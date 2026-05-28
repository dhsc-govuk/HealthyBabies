using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IDataCollectionSubmissionRepository
{
    Task<Option<DataCollectionSubmission>> GetByIdAsync(
        DataCollectionSubmissionId id,
        CancellationToken cancellationToken = default);

    Task<Option<DataCollectionSubmission>> GetByDataCollectionAndOrganisationAsync(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);

    Task<DataCollectionSubmission> AddAsync(
        DataCollectionSubmission entity,
        CancellationToken cancellationToken = default);

    Task<DataCollectionSubmission> UpdateAsync(
        DataCollectionSubmission entity,
        CancellationToken cancellationToken = default);
}