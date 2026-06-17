using Domain.ValueObjects;
using LanguageExt;

namespace Application.Users.Interfaces;

public record CreatedIdentity(string SubId);

public interface IUsersService
{
    Task<Option<string>> FindById(string userId, CancellationToken cancellationToken);
    Task<Option<string>> FindByEmail(string email, CancellationToken cancellationToken);
    Task<Either<Exception, CreatedIdentity>> AddWithPassword(
        Name userName,
        string email,
        bool isActive,
        CancellationToken cancellationToken);

    Task<Either<Exception, Unit>> Update(
        string id,
        Name userName,
        string email,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> Activate(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> Deactivate(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> Delete(
        string userId,
        CancellationToken cancellationToken = default);
}