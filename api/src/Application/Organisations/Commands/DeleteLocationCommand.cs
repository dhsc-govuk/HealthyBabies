using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Exceptions;
using Domain.Locations;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record DeleteLocationCommand : IRequest<Either<LocationException, LanguageExt.Unit>>
{
    public Guid LocationId { get; init; }
}

public class DeleteLocationCommandHandler(
    PermissionsService permissionsService,
    ILocationRepository locationRepository)
    : IRequestHandler<DeleteLocationCommand, Either<LocationException, LanguageExt.Unit>>
{
    public async Task<Either<LocationException, LanguageExt.Unit>> Handle(
        DeleteLocationCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var locationId = new LocationId(request.LocationId);
                var locationResult = await GetLocation(locationId, p, cancellationToken);
                return await locationResult.BindAsync<LocationException, Location, LanguageExt.Unit>(
                    location => DeleteLocationAsync(location, cancellationToken));
            },
            e => new LocationArgumentException(new LocationId(request.LocationId), e.Message));
    }

    private async Task<Either<LocationException, Location>> GetLocation(
        LocationId locationId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var location = await locationRepository.GetById(locationId, permission, cancellationToken);
        return location.Match<Either<LocationException, Location>>(
            l => l,
            () => new LocationDoesNotExistException(locationId));
    }

    private async Task<Either<LocationException, LanguageExt.Unit>> DeleteLocationAsync(
        Location location,
        CancellationToken cancellationToken)
    {
        try
        {
            await locationRepository.DeleteAsync(location, cancellationToken);
            return LanguageExt.Unit.Default;
        }
        catch (Exception exception)
        {
            return new LocationUnknownException(location.Id, exception);
        }
    }
}