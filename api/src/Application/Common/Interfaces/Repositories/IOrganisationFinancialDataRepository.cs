using Domain.Organisations;

namespace Application.Common.Interfaces.Repositories;

public interface IOrganisationFinancialDataRepository
{
    Task<OrganisationFinancialData> AddAsync(OrganisationFinancialData entity, CancellationToken cancellationToken = default);
    Task<OrganisationFinancialData> UpdateAsync(OrganisationFinancialData entity, CancellationToken cancellationToken = default);
}