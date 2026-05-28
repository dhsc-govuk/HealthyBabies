using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Services.Exceptions;
using Domain.Organisations;
using Domain.Services;
using LanguageExt;
using MediatR;

namespace Application.Services.Commands;

public record CreateServiceCommand : IRequest<Either<ServiceException, Service>>
{
    public string? Name { get; init; }
}

public class CreateServiceCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository)
    : IRequestHandler<CreateServiceCommand, Either<ServiceException, Service>>
{
    public async Task<Either<ServiceException, Service>> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p => await p.OrganisationId.MatchAsync(
                async orgId => await CheckIfExist(request.Name!, orgId, cancellationToken),
                () => Task.FromResult<Either<ServiceException, Service>>(
                    new ServiceArgumentException(ServiceId.Empty(), "Organisation id is missing"))),
            e => new ServiceArgumentException(ServiceId.Empty(), e.Message));
    }

    private async Task<Either<ServiceException, Service>> CheckIfExist(
        string name,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var serviceWithName = await serviceRepository.FindByNameAsync(
            name,
            organisationId,
            cancellationToken);

        return await serviceWithName.MatchAsync(
            s => new ServiceAlreadyExistsException(s.Id, name),
            () => CreateService(organisationId, name, cancellationToken));
    }

    private async Task<Either<ServiceException, Service>> CreateService(
        OrganisationId organisationId,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = Service.New(
                ServiceId.New(),
                organisationId,
                name);
            return await serviceRepository.AddAsync(service, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceUnknownException(ServiceId.Empty(), exception);
        }
    }
}