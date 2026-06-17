using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IFormFieldRepository
{
    Task<Option<FormField>> GetById(FormFieldId id, CancellationToken cancellationToken = default);
    Task<Option<FormField>> FindByFieldKeyAsync(DataCollectionFormModuleId moduleId, string fieldKey, CancellationToken cancellationToken = default);
    Task<int> GetMaxDisplayOrderForModule(DataCollectionFormModuleId moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormField>> GetByModuleIdTracking(DataCollectionFormModuleId moduleId, CancellationToken cancellationToken = default);
    Task<FormField> AddAsync(FormField entity, CancellationToken cancellationToken = default);
    Task<FormField> UpdateAsync(FormField entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(FormField entity, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedValuesAsync(FormFieldId fieldId, CancellationToken cancellationToken = default);
    Task ClearOptionsAsync(FormFieldId fieldId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}