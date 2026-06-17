using Application.Common;
using Application.Mfa.Dtos;
using Application.Users.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IAdminUsersControllerService
{
    Task<IEnumerable<AdminUserDto>> GetAll(CancellationToken cancellationToken);
    Task<Option<AdminUserDto>> Get(Guid userId, CancellationToken cancellationToken);
    Task<PaginatedResult<OrganisationUserDto>> GetOrganisationUsers(OrganisationUserQueryDto query, CancellationToken cancellationToken);
    Task<Option<OrganisationUserDto>> GetOrganisationUser(Guid userId, CancellationToken cancellationToken);
    Task<MfaStatusDto> GetMfaStatus(Guid userId, CancellationToken cancellationToken);
}