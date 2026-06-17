using Domain.DataCollections;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IDataCollectionFormModuleQueries
{
    Task<IReadOnlyList<DataCollectionFormModule>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Option<DataCollectionFormModule>> GetByIdWithFieldsAsync(DataCollectionFormModuleId id, CancellationToken cancellationToken = default);
    Task<Option<DataCollectionFormModule>> GetByCodeWithFieldsAsync(string code, CancellationToken cancellationToken = default);
}