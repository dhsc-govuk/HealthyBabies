using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceCategories.Exceptions;
using Domain.ServiceCategories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.ServiceCategories.Commands;

public record DeleteServiceCategoryCommand : IRequest<Either<ServiceCategoryException, Unit>>
{
    public Guid ServiceCategoryId { get; init; }
}

public class DeleteServiceCategoryCommandHandler(
    PermissionsService permissionsService,
    IServiceCategoryRepository serviceCategoryRepository)
    : IRequestHandler<DeleteServiceCategoryCommand, Either<ServiceCategoryException, Unit>>
{
    public async Task<Either<ServiceCategoryException, Unit>> Handle(
        DeleteServiceCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceCategoryId = new ServiceCategoryId(request.ServiceCategoryId);
                var serviceCategoryResult = await GetServiceCategory(serviceCategoryId, p, cancellationToken);

                return await serviceCategoryResult.BindAsync<ServiceCategoryException, ServiceCategory, Unit>(
                    serviceCategory => DeleteServiceCategory(serviceCategory, cancellationToken));
            },
            e => new ServiceCategoryArgumentException(new ServiceCategoryId(request.ServiceCategoryId), e.Message));
    }

    private async Task<Either<ServiceCategoryException, Unit>> DeleteServiceCategory(
        ServiceCategory serviceCategory,
        CancellationToken cancellationToken)
    {
        try
        {
            await serviceCategoryRepository.DeleteAsync(serviceCategory, cancellationToken);
            return Unit.Default;
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