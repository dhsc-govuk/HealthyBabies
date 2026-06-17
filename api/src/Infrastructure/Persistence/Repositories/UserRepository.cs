using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;
using Infrastructure.Persistence.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext)
    : RepositoryBase<User, UserId>(dbContext), IUserRepository, IUserQueries
{
    public Task<int> Count(CancellationToken cancellationToken = default)
    {
        return Context.Users.Where(x => x.Role == UserRole.Admin).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAdminUsers(CancellationToken cancellationToken = default)
    {
        return await Context.Users.Where(x => x.Role == UserRole.Admin).AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Option<User>> FindById(UserId id, CancellationToken cancellationToken = default)
    {
        var user = await Context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Role == UserRole.Admin && x.Id == id, cancellationToken);
        return user ?? Option<User>.None;
    }

    public async Task<Option<User>> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        return entity ?? Option<User>.None;
    }

    public async Task<Option<User>> FindDuplicateAsync(
        string targetEmail,
        UserId sourceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == targetEmail && x.Id != sourceId, cancellationToken);
        return entity ?? Option<User>.None;
    }

    public async Task<Option<User>> FindDeletedByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email && x.IsDeleted, cancellationToken);
        return entity ?? Option<User>.None;
    }

    public async Task<Option<User>> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<User>.None;
    }

    public async Task<Option<User>> GetByIdIgnoringFiltersAsync(
        UserId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<User>.None;
    }

    public Task<PaginatedResult<OrganisationUser>> GetOrganisationUser(
        Guid? organisationId,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var userQuery = Context.OrganisationUsers.Include(x => x.User).Include(o => o.Organisation).AsNoTracking();

        if (organisationId.HasValue)
        {
            var id = new OrganisationId(organisationId.Value);

            userQuery = userQuery.Where(x => x.OrganisationId == id);
        }

        return userQuery.PaginateAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<Option<OrganisationUser>> GetOrganisationUserById(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var entity = await Context.OrganisationUsers
            .Include(x => x.User)
            .Include(o => o.Organisation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return entity ?? Option<OrganisationUser>.None;
    }
}