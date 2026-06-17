using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IOrganisationContactQueries
{
    Task<IReadOnlyList<OrganisationContact>> GetByOrganisationIdAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<Option<OrganisationContact>> GetByIdAsync(OrganisationContactId id, CancellationToken cancellationToken = default);
    Task<int> CountByOrganisationIdAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
}