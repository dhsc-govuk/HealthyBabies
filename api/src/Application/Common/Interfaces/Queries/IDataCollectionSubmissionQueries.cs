using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IDataCollectionSubmissionQueries
{
    Task<IReadOnlyList<DataCollectionSubmission>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);

    Task<Option<DataCollectionSubmission>> GetByIdAsync(
        DataCollectionSubmissionId id,
        CancellationToken cancellationToken = default);

    Task<Option<DataCollectionSubmission>> GetByDataCollectionAndOrganisationAsync(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);

    Task<Option<DataCollectionSubmission>> GetByDataCollectionAndOrganisationWithSectionsAsync(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataCollectionSubmission>> GetByDataCollectionIdAsync(
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken = default);
}