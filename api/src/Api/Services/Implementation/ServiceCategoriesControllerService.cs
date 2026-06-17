using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceCategories.Dtos;
using Domain.ServiceCategories;
using LanguageExt;

namespace Api.Services.Implementation;

public class ServiceCategoriesControllerService(
    IServiceCategoryQueries serviceCategoryQueries,
    IServiceCategoryRepository serviceCategoryRepository)
    : IServiceCategoriesControllerService
{
    public async Task<IReadOnlyList<ServiceCategoryListDto>> GetAll(
        Permission permission,
        CancellationToken cancellationToken)
    {
        var serviceCategories = await serviceCategoryQueries.GetAll(permission, cancellationToken);
        return serviceCategories.Select(ServiceCategoryListDto.FromDomainModel).ToList();
    }

    public async Task<Option<ServiceCategoryDto>> Get(
        Permission permission,
        Guid serviceCategoryId,
        CancellationToken cancellationToken)
    {
        var serviceCategory = await serviceCategoryRepository.GetById(
            permission,
            new ServiceCategoryId(serviceCategoryId),
            cancellationToken);

        return serviceCategory.Map(ServiceCategoryDto.FromDomainModel);
    }
}