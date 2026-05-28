using Domain.DataCollections.Forms;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IDataSourceRepository
{
    Task<Option<DataSource>> GetByIdAsync(DataSourceId id, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByIdWithItemsAsync(DataSourceId id, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Option<DataSource>> GetByCodeWithItemsAsync(string code, CancellationToken cancellationToken = default);
    Task<DataSource> AddAsync(DataSource entity, CancellationToken cancellationToken = default);
    Task<DataSource> UpdateAsync(DataSource entity, CancellationToken cancellationToken = default);
}