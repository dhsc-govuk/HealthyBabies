using Application.Common.Permissions;
using Application.Organisations.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ILocationsControllerService
{
    Task<IEnumerable<LocationDto>> GetGlobal(CancellationToken cancellationToken);

    Task<IEnumerable<LocationDto>> GetAll(
        Permission permission,
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<Option<LocationDto>> Get(
        Permission permission,
        Guid locationId,
        CancellationToken cancellationToken);

    Task<Option<LocationHomeDto>> Totals(
        Permission permission,
        Guid locationId,
        CancellationToken cancellationToken);
}