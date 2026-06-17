using Application.Common.Permissions;
using Application.Organisations.Dtos;

namespace Api.Services.Abstract;

public interface IOrganisationManagerHomeControllerService
{
    Task<OrganisationAdminHomeDto> GetTotals(Permission permission, CancellationToken cancellationToken);
}