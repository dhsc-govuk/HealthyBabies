using Application.Common.Interfaces.Queries;
using Domain.ServiceCategoryForms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ServiceCategoryFormQuestionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ServiceCategoryFormQuestion, ServiceCategoryFormQuestionId>(dbContext), IServiceCategoryFormQuestionQueries
{
    public async Task<IReadOnlyList<ServiceCategoryFormQuestion>> GetAll(CancellationToken cancellationToken = default)
    {
        return await Context.ServiceCategoryFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Where(x => x.IsActive)
            .OrderBy(x => x.Step)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategoryFormQuestion>> GetByStep(int step, CancellationToken cancellationToken = default)
    {
        return await Context.ServiceCategoryFormQuestions
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Where(x => x.IsActive && x.Step == step)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }
}