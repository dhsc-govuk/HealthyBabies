using Application.Common.Permissions;
using Application.Users.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IOrganisationUsersControllerService
{
    Task<IEnumerable<OrganisationUserDto>> GetAll(
        Permission permission,
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<Option<OrganisationUserDto>> Get(
        Permission permission,
        Guid userId,
        CancellationToken cancellationToken);
}