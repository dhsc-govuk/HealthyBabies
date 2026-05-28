using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.ServiceForms;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ServiceFormQuestionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ServiceFormQuestion, ServiceFormQuestionId>(dbContext), IServiceFormQuestionRepository, IServiceFormQuestionQueries
{
    public async Task<IReadOnlyList<ServiceFormQuestion>> GetAll(CancellationToken cancellationToken = default)
    {
        return await Context.ServiceFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .OrderBy(x => x.Step)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceFormQuestion>> GetAllActive(CancellationToken cancellationToken = default)
    {
        return await Context.ServiceFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Where(x => x.IsActive)
            .OrderBy(x => x.Step)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceFormQuestion>> GetByStep(int step, CancellationToken cancellationToken = default)
    {
        return await Context.ServiceFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Where(x => x.IsActive && x.Step == step)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<ServiceFormQuestion>> GetById(ServiceFormQuestionId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.ServiceFormQuestions
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<ServiceFormQuestion>.None;
    }

    public async Task<Option<ServiceFormQuestion>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.ServiceFormQuestions
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<ServiceFormQuestion>.None;
    }

    public async Task<Option<ServiceFormQuestion>> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await Context.ServiceFormQuestions
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity ?? Option<ServiceFormQuestion>.None;
    }

    public async Task DeleteAsync(ServiceFormQuestion entity, CancellationToken cancellationToken = default)
    {
        Context.ServiceFormQuestions.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetMaxDisplayOrderForStep(int step, CancellationToken cancellationToken = default)
    {
        var maxOrder = await Context.ServiceFormQuestions
            .Where(x => x.Step == step)
            .MaxAsync(x => (int?)x.DisplayOrder, cancellationToken);
        return maxOrder ?? 0;
    }

    public async Task<IReadOnlyList<ServiceFormQuestion>> GetByStepTracking(int step, CancellationToken cancellationToken = default)
    {
        return await Context.ServiceFormQuestions
            .Where(x => x.Step == step)
            .ToListAsync(cancellationToken);
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }
}