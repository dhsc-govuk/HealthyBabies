using Application.Common.Permissions;
using Application.Organisations.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IOrganisationsControllerService
{
    Task<IEnumerable<OrganisationDto>> GetAll(CancellationToken cancellationToken);
    Task<Option<OrganisationDto>> Get(Guid organisationId, CancellationToken cancellationToken);

    Task<Option<OrganisationHomeDto>> Totals(
        Permission permission,
        Guid organisationId,
        CancellationToken cancellationToken);
}