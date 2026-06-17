using Domain.OrganisationUsers;
using Domain.Users;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IUserQueries
{
    Task<int> Count(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAdminUsers(CancellationToken cancellationToken = default);
    Task<Option<User>> FindById(UserId id, CancellationToken cancellationToken = default);

    Task<PaginatedResult<OrganisationUser>> GetOrganisationUser(
        Guid? organisationId,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken);

    Task<Option<OrganisationUser>> GetOrganisationUserById(
        UserId userId,
        CancellationToken cancellationToken);
}