using Domain.DataCollections.Forms;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IDataSourceQueries
{
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSource>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByIdAsync(DataSourceId id, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByIdWithItemsAsync(DataSourceId id, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByCodeWithItemsAsync(string code, CancellationToken cancellationToken = default);
}