using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections.Forms;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DataSourceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DataSource, DataSourceId>(dbContext), IDataSourceRepository, IDataSourceQueries
{
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Context.DataSources.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.DataSources
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataSource>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.DataSources
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<DataSource>> GetByIdAsync(DataSourceId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<DataSource>.None;
    }

    public async Task<Option<DataSource>> GetByIdWithItemsAsync(DataSourceId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataSources
            .Include(x => x.Items.Where(i => i.IsActive).OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<DataSource>.None;
    }

    public async Task<Option<DataSource>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<DataSource>.None;
    }

    public async Task<Option<DataSource>> GetByCodeWithItemsAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataSources
            .Include(x => x.Items.Where(i => i.IsActive).OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<DataSource>.None;
    }
}