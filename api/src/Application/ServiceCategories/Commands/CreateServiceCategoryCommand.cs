using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceCategories.Exceptions;
using Domain.Organisations;
using Domain.ServiceCategories;
using LanguageExt;
using MediatR;

namespace Application.ServiceCategories.Commands;

public record CreateServiceCategoryCommand : IRequest<Either<ServiceCategoryException, ServiceCategory>>
{
    public string? CategoryCode { get; init; }
    public string? CategoryName { get; init; }
}

public class CreateServiceCategoryCommandHandler(
    PermissionsService permissionsService,
    IServiceCategoryRepository serviceCategoryRepository)
    : IRequestHandler<CreateServiceCategoryCommand, Either<ServiceCategoryException, ServiceCategory>>
{
    public async Task<Either<ServiceCategoryException, ServiceCategory>> Handle(
        CreateServiceCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p => await p.OrganisationId.MatchAsync(
                async orgId => await CheckIfExist(request.CategoryCode!, request.CategoryName!, orgId, cancellationToken),
                () => Task.FromResult<Either<ServiceCategoryException, ServiceCategory>>(
                    new ServiceCategoryArgumentException(ServiceCategoryId.Empty(), "Organisation id is missing"))),
            e => new ServiceCategoryArgumentException(ServiceCategoryId.Empty(), e.Message));
    }

    private async Task<Either<ServiceCategoryException, ServiceCategory>> CheckIfExist(
        string categoryCode,
        string categoryName,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var existingCategory = await serviceCategoryRepository.FindByCategoryCodeAsync(
            categoryCode,
            organisationId,
            cancellationToken);

        return await existingCategory.MatchAsync(
            s => new ServiceCategoryAlreadyExistsException(s.Id, categoryCode),
            () => CreateServiceCategory(organisationId, categoryCode, categoryName, cancellationToken));
    }

    private async Task<Either<ServiceCategoryException, ServiceCategory>> CreateServiceCategory(
        OrganisationId organisationId,
        string categoryCode,
        string categoryName,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceCategory = ServiceCategory.New(
                ServiceCategoryId.New(),
                organisationId,
                categoryCode,
                categoryName);
            return await serviceCategoryRepository.AddAsync(serviceCategory, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceCategoryUnknownException(ServiceCategoryId.Empty(), exception);
        }
    }
}