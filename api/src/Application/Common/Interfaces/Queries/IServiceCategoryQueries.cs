using Application.Common.Permissions;
using Domain.ServiceCategories;

namespace Application.Common.Interfaces.Queries;

public interface IServiceCategoryQueries
{
    Task<IReadOnlyList<ServiceCategory>> GetAll(Permission permission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetByOrganisationId(Guid organisationId, CancellationToken cancellationToken = default);
    Task<int> Count(Permission permission, CancellationToken cancellationToken = default);
}