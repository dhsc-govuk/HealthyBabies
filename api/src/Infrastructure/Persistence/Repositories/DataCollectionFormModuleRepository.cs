using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DataCollectionFormModuleRepository(ApplicationDbContext dbContext)
    : RepositoryBase<DataCollectionFormModule, DataCollectionFormModuleId>(dbContext),
      IDataCollectionFormModuleRepository,
      IDataCollectionFormModuleQueries
{
    private readonly ApplicationDbContext _context = dbContext;

    public async Task<IReadOnlyList<DataCollectionFormModule>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DataCollectionFormModules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SectionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<DataCollectionFormModule>> GetByIdWithFieldsAsync(DataCollectionFormModuleId id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataCollectionFormModules
            .AsNoTracking()
            .Include(x => x.Sections.OrderBy(s => s.SectionNumber))
            .Include(x => x.Fields)
                .ThenInclude(f => f.Options)
            .Include(x => x.Fields)
                .ThenInclude(f => f.FormSection)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<DataCollectionFormModule>.None;
    }

    public async Task<Option<DataCollectionFormModule>> GetByCodeWithFieldsAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DataCollectionFormModules
            .AsNoTracking()
            .Include(x => x.Sections.OrderBy(s => s.SectionNumber))
            .Include(x => x.Fields)
                .ThenInclude(f => f.Options)
            .Include(x => x.Fields)
                .ThenInclude(f => f.FormSection)
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

        return entity ?? Option<DataCollectionFormModule>.None;
    }

    public async Task<Option<DataCollectionFormModule>> GetById(DataCollectionFormModuleId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionFormModules
            .Include(x => x.Sections.OrderBy(s => s.SectionNumber))
            .Include(x => x.Fields)
                .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<DataCollectionFormModule>.None;
    }

    public async Task<IReadOnlyList<DataCollectionFormModule>> GetAll(CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionFormModules
            .AsNoTracking()
            .Include(x => x.Sections.OrderBy(s => s.SectionNumber))
            .OrderBy(x => x.SectionNumber)
            .ToListAsync(cancellationToken);
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Option<DataCollectionFormModule>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.DataCollectionFormModules
            .Include(x => x.Sections.OrderBy(s => s.SectionNumber))
            .Include(x => x.Fields)
                .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        return entity ?? Option<DataCollectionFormModule>.None;
    }

    public new async Task<DataCollectionFormModule> AddAsync(DataCollectionFormModule entity, CancellationToken cancellationToken = default)
    {
        await Context.DataCollectionFormModules.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(DataCollectionFormModule entity, CancellationToken cancellationToken = default)
    {
        Context.DataCollectionFormModules.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCode(string code, CancellationToken cancellationToken = default)
    {
        return await Context.DataCollectionFormModules
            .AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<int> GetNextSectionNumber(CancellationToken cancellationToken = default)
    {
        var maxSectionNumber = await Context.DataCollectionFormModules
            .MaxAsync(x => (int?)x.SectionNumber, cancellationToken);
        return (maxSectionNumber ?? 0) + 1;
    }
}