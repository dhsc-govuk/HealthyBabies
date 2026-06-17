using Domain.Users;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<Option<User>> FindById(UserId id, CancellationToken cancellationToken = default);
    Task<Option<User>> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Option<User>> FindDeletedByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Option<User>> FindDuplicateAsync(string targetEmail, UserId sourceId, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User entity, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default);
    Task<Option<User>> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<Option<User>> GetByIdIgnoringFiltersAsync(UserId id, CancellationToken cancellationToken = default);
}