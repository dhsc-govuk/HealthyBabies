using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IFormFieldQueries
{
    Task<IReadOnlyList<FormField>> GetAll(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormField>> GetAllActive(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormField>> GetByModuleId(DataCollectionFormModuleId moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormField>> GetByModuleCode(string moduleCode, CancellationToken cancellationToken = default);
    Task<Option<FormField>> GetById(FormFieldId id, CancellationToken cancellationToken = default);
    Task<Option<FormField>> GetByFieldKey(DataCollectionFormModuleId moduleId, string fieldKey, CancellationToken cancellationToken = default);
}