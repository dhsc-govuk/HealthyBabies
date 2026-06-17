using Domain.Users;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IUserMfaRepository
{
    Task<Option<UserMfa>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Dictionary<UserId, UserMfa>> GetByUserIdsAsync(IEnumerable<UserId> userIds, CancellationToken cancellationToken = default);
    Task<UserMfa> AddAsync(UserMfa userMfa, CancellationToken cancellationToken = default);
    Task<UserMfa> UpdateAsync(UserMfa userMfa, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserMfa userMfa, CancellationToken cancellationToken = default);
}