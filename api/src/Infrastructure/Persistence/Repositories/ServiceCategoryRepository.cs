using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ServiceCategories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ServiceCategoryRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ServiceCategory, ServiceCategoryId>(dbContext), IServiceCategoryRepository, IServiceCategoryQueries
{
    public async Task<IReadOnlyList<ServiceCategory>> GetAll(
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await permission.OrganisationId.MatchAsync<OrganisationId, IReadOnlyList<ServiceCategory>>(
            async orgId => await Context.ServiceCategories
                .AsNoTracking()
                .Where(x => x.OrganisationId == orgId)
                .OrderBy(x => x.CategoryName)
                .ToListAsync(cancellationToken),
            () => []);
    }

    public async Task<Option<ServiceCategory>> GetById(
        Permission permission,
        ServiceCategoryId id,
        CancellationToken cancellationToken)
    {
        var entity = await Context.ServiceCategories
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers.OrderBy(a => a.Step).ThenBy(a => a.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return Option<ServiceCategory>.None;
        }

        var hasAccess = permission.OrganisationId.Match(
            orgId => entity.OrganisationId == orgId,
            () => false);

        return hasAccess ? entity : Option<ServiceCategory>.None;
    }

    public async Task<Option<ServiceCategory>> GetByIdForUpdate(
        Permission permission,
        ServiceCategoryId id,
        CancellationToken cancellationToken)
    {
        var entity = await Context.ServiceCategories
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return Option<ServiceCategory>.None;
        }

        var hasAccess = permission.OrganisationId.Match(
            orgId => entity.OrganisationId == orgId,
            () => false);

        return hasAccess ? entity : Option<ServiceCategory>.None;
    }

    public async Task<Option<ServiceCategory>> FindByCategoryCodeAsync(
        string categoryCode,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var entity = await Context.ServiceCategories
            .FirstOrDefaultAsync(x => x.CategoryCode == categoryCode && x.OrganisationId == organisationId, cancellationToken);
        return entity ?? Option<ServiceCategory>.None;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetByOrganisationId(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await Context.ServiceCategories
            .AsNoTracking()
            .Where(x => x.OrganisationId == new OrganisationId(organisationId))
            .OrderBy(x => x.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> Count(
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await permission.OrganisationId.MatchAsync<OrganisationId, int>(
            async orgId => await Context.ServiceCategories
                .AsNoTracking()
                .CountAsync(x => x.OrganisationId == orgId, cancellationToken),
            () => 0);
    }

    public async Task DeleteAsync(ServiceCategory entity, CancellationToken cancellationToken = default)
    {
        Context.ServiceCategories.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}