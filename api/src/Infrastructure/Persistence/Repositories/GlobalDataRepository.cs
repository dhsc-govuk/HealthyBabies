using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Systems;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class GlobalDataRepository(ApplicationDbContext dbContext)
    : RepositoryBase<GlobalData, GlobalDataId>(dbContext), IGlobalDataRepository, IGlobalDataQueries
{
    public async Task<IReadOnlyList<GlobalData>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.GlobalData
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GlobalData>> GetByEntityAsync(
        string entity,
        CancellationToken cancellationToken = default)
    {
        return await Context.GlobalData
            .Where(x => x.Entity == entity)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<GlobalData>> GetByIdAsync(
        GlobalDataId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.GlobalData
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<GlobalData>.None;
    }
}