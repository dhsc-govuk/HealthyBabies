using Domain.DataCollections;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IDataCollectionRepository
{
    Task<Option<DataCollection>> GetByIdAsync(DataCollectionId id, CancellationToken cancellationToken = default);
    Task<Option<DataCollection>> GetByIdWithLocalAuthoritiesAsync(DataCollectionId id, CancellationToken cancellationToken = default);
    Task<Option<DataCollection>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Option<DataCollection>> FindDuplicateAsync(string targetName, DataCollectionId sourceId, CancellationToken cancellationToken = default);
    Task<DataCollection> AddAsync(DataCollection entity, CancellationToken cancellationToken = default);
    Task<DataCollection> UpdateAsync(DataCollection entity, CancellationToken cancellationToken = default);
    Task UpdateFormModuleAssignmentsAsync(DataCollectionId dataCollectionId, IReadOnlyList<Guid> formModuleIds, CancellationToken cancellationToken = default);
}