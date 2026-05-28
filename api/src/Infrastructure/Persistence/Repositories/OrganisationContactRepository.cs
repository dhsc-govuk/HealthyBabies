using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Organisations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrganisationContactRepository(ApplicationDbContext dbContext)
    : RepositoryBase<OrganisationContact, OrganisationContactId>(dbContext), IOrganisationContactRepository, IOrganisationContactQueries
{
    public async Task<IReadOnlyList<OrganisationContact>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.OrganisationContacts
            .AsNoTracking()
            .Where(x => x.OrganisationId == organisationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<OrganisationContact>> GetByIdAsync(
        OrganisationContactId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.OrganisationContacts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<OrganisationContact>.None;
    }

    public async Task<int> CountByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.OrganisationContacts
            .CountAsync(x => x.OrganisationId == organisationId, cancellationToken);
    }
}