using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IDataCollectionQueries
{
    Task<int> Count(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCollection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Option<DataCollection>> GetByIdAsync(DataCollectionId id, CancellationToken cancellationToken = default);
    Task<Option<DataCollection>> GetByIdWithLocalAuthoritiesAsync(DataCollectionId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataCollection>> GetByOrganisationIdAsync(OrganisationId organisationId, CancellationToken cancellationToken = default);
}