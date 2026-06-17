using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ServiceCategories;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IServiceCategoryRepository
{
    Task<Option<ServiceCategory>> GetById(Permission permission, ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<Option<ServiceCategory>> GetByIdForUpdate(Permission permission, ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<Option<ServiceCategory>> FindByCategoryCodeAsync(string categoryCode, OrganisationId organisationId, CancellationToken cancellationToken = default);
    Task<ServiceCategory> AddAsync(ServiceCategory entity, CancellationToken cancellationToken = default);
    Task<ServiceCategory> UpdateAsync(ServiceCategory entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(ServiceCategory entity, CancellationToken cancellationToken = default);
}