using Application.Common.Permissions;
using Application.Organisations.Dtos;

namespace Api.Services.Abstract;

public interface ILocationAdminHomeControllerService
{
    Task<LocationAdminHomeDto> Totals(Permission permission, CancellationToken cancellationToken);
}