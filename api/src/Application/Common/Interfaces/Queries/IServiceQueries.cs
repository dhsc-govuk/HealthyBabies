using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ServiceCategories;
using Domain.Services;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IServiceQueries
{
    Task<IReadOnlyList<Service>> GetAll(
        Permission permission,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Service>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);

    Task<Option<Service>> GetById(
        Permission permission,
        ServiceId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Service>> GetAllForExport(CancellationToken cancellationToken = default);

    Task<int> Count(
        Permission permission,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default);
}