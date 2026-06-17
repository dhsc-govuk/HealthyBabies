using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Services.Exceptions;
using Domain.Services;
using LanguageExt;
using MediatR;

namespace Application.Services.Commands;

public record CompleteServiceCommand : IRequest<Either<ServiceException, Service>>
{
    public Guid ServiceId { get; init; }
}

public class CompleteServiceCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository)
    : IRequestHandler<CompleteServiceCommand, Either<ServiceException, Service>>
{
    public async Task<Either<ServiceException, Service>> Handle(
        CompleteServiceCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceId = new ServiceId(request.ServiceId);
                var serviceResult = await GetService(serviceId, p, cancellationToken);

                return await serviceResult.BindAsync<ServiceException, Service, Service>(service =>
                    CompleteService(service, cancellationToken));
            },
            e => new ServiceArgumentException(new ServiceId(request.ServiceId), e.Message));
    }

    private async Task<Either<ServiceException, Service>> CompleteService(
        Service service,
        CancellationToken cancellationToken)
    {
        try
        {
            service.Complete();
            return await serviceRepository.UpdateAsync(service, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceUnknownException(service.Id, exception);
        }
    }

    private async Task<Either<ServiceException, Service>> GetService(
        ServiceId serviceId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdForUpdate(permission, serviceId, cancellationToken);
        return service.Match<Either<ServiceException, Service>>(
            s => s,
            () => new ServiceDoesNotExistException(serviceId));
    }
}