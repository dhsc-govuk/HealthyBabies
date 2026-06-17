using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Services.Exceptions;
using Domain.Services;
using LanguageExt;
using MediatR;

namespace Application.Services.Commands;

public record DeleteServiceCommand : IRequest<Either<ServiceException, LanguageExt.Unit>>
{
    public Guid ServiceId { get; init; }
}

public class DeleteServiceCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository)
    : IRequestHandler<DeleteServiceCommand, Either<ServiceException, LanguageExt.Unit>>
{
    public async Task<Either<ServiceException, LanguageExt.Unit>> Handle(
        DeleteServiceCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceId = new ServiceId(request.ServiceId);
                var serviceResult = await GetService(serviceId, p, cancellationToken);

                return await serviceResult.BindAsync<ServiceException, Service, LanguageExt.Unit>(service =>
                    DeleteService(service, cancellationToken));
            },
            e => new ServiceArgumentException(new ServiceId(request.ServiceId), e.Message));
    }

    private async Task<Either<ServiceException, LanguageExt.Unit>> DeleteService(
        Service service,
        CancellationToken cancellationToken)
    {
        try
        {
            await serviceRepository.DeleteAsync(service, cancellationToken);
            return LanguageExt.Unit.Default;
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
        var service = await serviceRepository.GetById(permission, serviceId, cancellationToken);
        return service.Match<Either<ServiceException, Service>>(
            s => s,
            () => new ServiceDoesNotExistException(serviceId));
    }
}