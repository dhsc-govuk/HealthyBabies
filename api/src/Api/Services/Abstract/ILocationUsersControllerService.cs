using Application.Common.Permissions;
using Application.Users.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface ILocationUsersControllerService
{
    Task<IEnumerable<UserDto>> GetAll(
        Permission permission,
        Guid locationId,
        CancellationToken cancellationToken);

    Task<Option<UserDto>> Get(
        Permission permission,
        Guid locationUserId,
        CancellationToken cancellationToken);
}