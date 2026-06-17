using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ServiceCategories;
using Domain.Services;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ServiceRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Service, ServiceId>(dbContext), IServiceRepository, IServiceQueries
{
    public async Task<IReadOnlyList<Service>> GetAllForExport(CancellationToken cancellationToken = default)
    {
        return await Context.Services
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers.OrderBy(a => a.Step).ThenBy(a => a.DisplayOrder))
            .OrderBy(x => x.Organisation!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetAll(
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await permission.OrganisationId.MatchAsync<OrganisationId, IReadOnlyList<Service>>(
            async orgId => await Context.Services
                .AsNoTracking()
                .Where(x => x.OrganisationId == orgId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken),
            () => []);
    }

    public async Task<Option<Service>> GetById(
        Permission permission,
        ServiceId id,
        CancellationToken cancellationToken)
    {
        var entity = await Context.Services
            .AsNoTracking()
            .Include(x => x.Organisation)
            .Include(x => x.Answers.OrderBy(a => a.Step).ThenBy(a => a.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return Option<Service>.None;
        }

        var hasAccess = permission.OrganisationId.Match(
            orgId => entity.OrganisationId == orgId,
            () => false);

        return hasAccess ? entity : Option<Service>.None;
    }

    public async Task<Option<Service>> GetByIdForUpdate(
        Permission permission,
        ServiceId id,
        CancellationToken cancellationToken)
    {
        // Don't use AsNoTracking since we'll be updating this entity
        // Don't include Organisation to avoid tracking conflicts
        var entity = await Context.Services
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return Option<Service>.None;
        }

        var hasAccess = permission.OrganisationId.Match(
            orgId => entity.OrganisationId == orgId,
            () => false);

        return hasAccess ? entity : Option<Service>.None;
    }

    public async Task<Option<Service>> FindByNameAsync(string name, OrganisationId organisationId, CancellationToken cancellationToken)
    {
        var entity = await Context.Services
            .FirstOrDefaultAsync(x => x.Name == name && x.OrganisationId == organisationId, cancellationToken);
        return entity ?? Option<Service>.None;
    }

    public async Task<Option<Service>> FindDuplicateAsync(
        string targetName,
        OrganisationId organisationId,
        ServiceId sourceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Services
            .FirstOrDefaultAsync(x => x.Name == targetName && x.OrganisationId == organisationId && x.Id != sourceId, cancellationToken);
        return entity ?? Option<Service>.None;
    }

    public async Task<IReadOnlyList<Service>> FindByNamesAsync(
        IReadOnlyList<string> names,
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedNames = names.Select(n => n.ToLowerInvariant()).ToList();
        return await Context.Services
            .Include(x => x.Answers)
            .Where(x => x.OrganisationId == organisationId &&
                        normalizedNames.Contains(x.Name.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> Count(
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await permission.OrganisationId.MatchAsync<OrganisationId, int>(
            async orgId => await Context.Services
                .AsNoTracking()
                .CountAsync(x => x.OrganisationId == orgId, cancellationToken),
            () => 0);
    }

    public async Task<IReadOnlyList<Service>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Services
            .AsNoTracking()
            .Include(x => x.Answers.OrderBy(a => a.Step).ThenBy(a => a.DisplayOrder))
            .Where(x => x.OrganisationId == organisationId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Service entity, CancellationToken cancellationToken = default)
    {
        Context.Services.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.ServiceCategories
            .AsNoTracking()
            .Include(x => x.Answers.OrderBy(a => a.Step).ThenBy(a => a.DisplayOrder))
            .Where(x => x.OrganisationId == organisationId)
            .OrderBy(x => x.CategoryName)
            .ToListAsync(cancellationToken);
    }
}