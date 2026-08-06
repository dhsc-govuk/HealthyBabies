using Domain.Organisations;

namespace Application.Common.Interfaces.Queries;

public interface IOrganisationFinancialDataQueries
{
    Task<IReadOnlyList<OrganisationFinancialData>> GetAllByOrganisationIdAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
}