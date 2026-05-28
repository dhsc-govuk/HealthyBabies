using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrganisationRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Organisation, OrganisationId>(dbContext), IOrganisationRepository, IOrganisationQueries
{
    public Task<int> Count(CancellationToken cancellationToken = default)
    {
        return Context.Organisations.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Organisation>> GetOrganisations(CancellationToken cancellationToken = default)
    {
        return await Context.Organisations.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Option<Organisation>> GetOrganisationById(OrganisationId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Organisations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<Organisation>.None;
    }

    public async Task<Option<Organisation>> GetOrganisationById(OrganisationId id, Permission permission, CancellationToken cancellationToken = default)
    {
        var entity = await GetQuery(permission.UserRole, permission.OrganisationId).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<Organisation>.None;
    }

    public async Task<Option<Organisation>> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Organisations.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        return entity ?? Option<Organisation>.None;
    }

    public async Task<Option<Organisation>> FindDuplicateAsync(string targetName, OrganisationId sourceId, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Organisations.AsNoTracking().FirstOrDefaultAsync(x => x.Name == targetName && x.Id != sourceId, cancellationToken);
        return entity ?? Option<Organisation>.None;
    }

    private IQueryable<Organisation> GetQuery(UserRole userRole, Option<OrganisationId> organisationId)
    {
        if (userRole.Equals(UserRole.Admin))
        {
            return Context.Organisations;
        }

        if (userRole.Equals(UserRole.OrganisationAdmin) && organisationId.IsSome)
        {
            return organisationId.Match(
                orgId => Context.Organisations.Where(x => x.Id == orgId),
                () => Array.Empty<Organisation>().AsQueryable());
        }

        return Array.Empty<Organisation>().AsQueryable();
    }
}