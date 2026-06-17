using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IOrganisationQueries
{
    Task<int> Count(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Organisation>> GetOrganisations(CancellationToken cancellationToken = default);
    Task<Option<Organisation>> GetOrganisationById(OrganisationId id, CancellationToken cancellationToken = default);
}