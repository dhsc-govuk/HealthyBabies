using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceCategories.Exceptions;
using Domain.ServiceCategories;
using LanguageExt;
using MediatR;

namespace Application.ServiceCategories.Commands;

public record CompleteServiceCategoryCommand : IRequest<Either<ServiceCategoryException, ServiceCategory>>
{
    public Guid ServiceCategoryId { get; init; }
}

public class CompleteServiceCategoryCommandHandler(
    PermissionsService permissionsService,
    IServiceCategoryRepository serviceCategoryRepository)
    : IRequestHandler<CompleteServiceCategoryCommand, Either<ServiceCategoryException, ServiceCategory>>
{
    public async Task<Either<ServiceCategoryException, ServiceCategory>> Handle(
        CompleteServiceCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceCategoryId = new ServiceCategoryId(request.ServiceCategoryId);
                var serviceCategoryResult = await GetServiceCategory(serviceCategoryId, p, cancellationToken);

                return await serviceCategoryResult.BindAsync<ServiceCategoryException, ServiceCategory, ServiceCategory>(
                    serviceCategory => CompleteServiceCategory(serviceCategory, cancellationToken));
            },
            e => new ServiceCategoryArgumentException(new ServiceCategoryId(request.ServiceCategoryId), e.Message));
    }

    private async Task<Either<ServiceCategoryException, ServiceCategory>> CompleteServiceCategory(
        ServiceCategory serviceCategory,
        CancellationToken cancellationToken)
    {
        try
        {
            serviceCategory.Complete();
            return await serviceCategoryRepository.UpdateAsync(serviceCategory, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceCategoryUnknownException(serviceCategory.Id, exception);
        }
    }

    private async Task<Either<ServiceCategoryException, ServiceCategory>> GetServiceCategory(
        ServiceCategoryId serviceCategoryId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var serviceCategory = await serviceCategoryRepository.GetByIdForUpdate(permission, serviceCategoryId, cancellationToken);
        return serviceCategory.Match<Either<ServiceCategoryException, ServiceCategory>>(
            s => s,
            () => new ServiceCategoryDoesNotExistException(serviceCategoryId));
    }
}