using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IFormSubmissionRepository
{
    Task<Option<FormSubmission>> GetByIdAsync(FormSubmissionId id, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByIdWithValuesAsync(FormSubmissionId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleAndEntityTypeAsync(DataCollectionFormModuleId formModuleId, string entityType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleAndDataCollectionAsync(DataCollectionFormModuleId formModuleId, DataCollectionId dataCollectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationAndDataCollectionAsync(DataCollectionFormModuleId formModuleId, OrganisationId organisationId, DataCollectionId dataCollectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationAndEntityTypeAsync(DataCollectionFormModuleId formModuleId, OrganisationId organisationId, string entityType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(DataCollectionFormModuleId formModuleId, OrganisationId organisationId, DataCollectionId dataCollectionId, string entityType, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByFormModuleAndServiceAsync(DataCollectionFormModuleId formModuleId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByFormModuleDataCollectionAndServiceAsync(DataCollectionFormModuleId formModuleId, DataCollectionId dataCollectionId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByFormModuleAndServiceCategoryAsync(DataCollectionFormModuleId formModuleId, Guid serviceCategoryId, CancellationToken cancellationToken = default);
    Task<Option<FormSubmission>> GetByFormModuleDataCollectionAndServiceCategoryAsync(DataCollectionFormModuleId formModuleId, DataCollectionId dataCollectionId, Guid serviceCategoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetByOrganisationIdAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormSubmission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(FormSubmissionId id, CancellationToken cancellationToken = default);
    Task<FormSubmission> AddAsync(FormSubmission entity, CancellationToken cancellationToken = default);
    Task<FormSubmission> UpdateAsync(FormSubmission entity, CancellationToken cancellationToken = default);
}