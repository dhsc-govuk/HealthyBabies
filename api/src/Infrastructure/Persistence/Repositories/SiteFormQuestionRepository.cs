using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.SiteForms;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SiteFormQuestionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SiteFormQuestion, SiteFormQuestionId>(dbContext), ISiteFormQuestionRepository, ISiteFormQuestionQueries
{
    public async Task<IReadOnlyList<SiteFormQuestion>> GetAll(CancellationToken cancellationToken = default)
    {
        return await Context.SiteFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SiteFormQuestion>> GetAllActive(CancellationToken cancellationToken = default)
    {
        return await Context.SiteFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<SiteFormQuestion>> GetById(SiteFormQuestionId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.SiteFormQuestions
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<SiteFormQuestion>.None;
    }

    public async Task<Option<SiteFormQuestion>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.SiteFormQuestions
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<SiteFormQuestion>.None;
    }

    public async Task<Option<SiteFormQuestion>> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.SiteFormQuestions
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<SiteFormQuestion>.None;
    }

    public async Task DeleteAsync(SiteFormQuestion entity, CancellationToken cancellationToken = default)
    {
        Context.SiteFormQuestions.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetMaxDisplayOrder(CancellationToken cancellationToken = default)
    {
        var maxOrder = await Context.SiteFormQuestions
            .MaxAsync(x => (int?)x.DisplayOrder, cancellationToken);
        return maxOrder ?? 0;
    }

    public async Task<IReadOnlyList<SiteFormQuestion>> GetAllTracking(CancellationToken cancellationToken = default)
    {
        return await Context.SiteFormQuestions
            .ToListAsync(cancellationToken);
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }
}