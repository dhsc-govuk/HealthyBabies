using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IFormSubmissionQueries
{
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByIdAsync(FormSubmissionId id, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByIdWithValuesAsync(FormSubmissionId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleIdAsync(DataCollectionFormModuleId formModuleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByStatusAsync(SubmissionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetDraftsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetPendingReviewAsync(CancellationToken cancellationToken = default);
}