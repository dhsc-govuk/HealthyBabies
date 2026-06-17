using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrganisationUserRepository(ApplicationDbContext dbContext)
    : RepositoryBase<OrganisationUser, OrganisationUserId>(dbContext), IOrganisationUserRepository,
        IOrganisationUserQueries
{
    public Task<int> Count(Option<OrganisationId> id, Permission permission, CancellationToken cancellationToken)
    {
        var query = GetQuery(permission.UserRole, permission.OrganisationId);
        return id.MatchAsync(orgId => query.Where(x => x.OrganisationId == orgId).CountAsync(cancellationToken), () => query.CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<OrganisationUser>> GetUsers(OrganisationId id, Permission permission, CancellationToken cancellationToken)
    {
        return await GetQuery(permission.UserRole, permission.OrganisationId)
            .Include(x => x.User)
            .Where(x => x.OrganisationId == id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<OrganisationUser>> GetOrganisationUserById(OrganisationUserId id, Permission permission, CancellationToken cancellationToken = default)
    {
        var entity = await GetQuery(permission.UserRole, permission.OrganisationId)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<OrganisationUser>.None;
    }

    public async Task<Option<OrganisationUser>> FindByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var entity = await Context.OrganisationUsers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return entity ?? Option<OrganisationUser>.None;
    }

    private IQueryable<OrganisationUser> GetQuery(UserRole userRole, Option<OrganisationId> organisationId)
    {
        if (userRole.Equals(UserRole.Admin))
        {
            return Context.OrganisationUsers;
        }

        if (userRole.Equals(UserRole.OrganisationAdmin) && organisationId.IsSome)
        {
            return organisationId.Match(
                orgId => Context.OrganisationUsers.Where(x => x.OrganisationId == orgId),
                () => Array.Empty<OrganisationUser>().AsQueryable());
        }

        return Array.Empty<OrganisationUser>().AsQueryable();
    }
}