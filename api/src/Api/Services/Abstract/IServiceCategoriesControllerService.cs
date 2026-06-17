using Application.Common.Permissions;
using Application.ServiceCategories.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IServiceCategoriesControllerService
{
    Task<IReadOnlyList<ServiceCategoryListDto>> GetAll(Permission permission, CancellationToken cancellationToken);
    Task<Option<ServiceCategoryDto>> Get(Permission permission, Guid serviceCategoryId, CancellationToken cancellationToken);
}