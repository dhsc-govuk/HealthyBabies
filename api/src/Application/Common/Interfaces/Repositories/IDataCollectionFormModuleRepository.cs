using Domain.DataCollections;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IDataCollectionFormModuleRepository
{
    Task<Option<DataCollectionFormModule>> GetById(DataCollectionFormModuleId id, CancellationToken cancellationToken = default);
    Task<Option<DataCollectionFormModule>> GetByCode(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCollectionFormModule>> GetAll(CancellationToken cancellationToken = default);
    Task<DataCollectionFormModule> AddAsync(DataCollectionFormModule entity, CancellationToken cancellationToken = default);
    Task<DataCollectionFormModule> UpdateAsync(DataCollectionFormModule entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(DataCollectionFormModule entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCode(string code, CancellationToken cancellationToken = default);
    Task<int> GetNextSectionNumber(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}