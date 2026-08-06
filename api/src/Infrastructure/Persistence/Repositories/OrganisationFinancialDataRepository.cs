using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Organisations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrganisationFinancialDataRepository(ApplicationDbContext dbContext)
    : RepositoryBase<OrganisationFinancialData, OrganisationFinancialDataId>(dbContext),
        IOrganisationFinancialDataRepository,
        IOrganisationFinancialDataQueries
{
    public async Task<IReadOnlyList<OrganisationFinancialData>> GetAllByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.OrganisationFinancialData
            .AsNoTracking()
            .Where(x => x.OrganisationId == organisationId)
            .ToListAsync(cancellationToken);
    }
}