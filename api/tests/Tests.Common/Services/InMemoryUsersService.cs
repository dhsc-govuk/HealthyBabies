using Application.Users.Interfaces;
using Domain.ValueObjects;
using LanguageExt;

namespace Tests.Common.Services;

public class InMemoryUsersService : IUsersService
{
    public Task<Option<string>> FindById(string userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<Option<string>>(Guid.NewGuid().ToString());
    }

    public Task<Option<string>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        return Task.FromResult<Option<string>>(Guid.NewGuid().ToString());
    }

    public Task<Either<Exception, CreatedIdentity>> AddWithPassword(Name userName, string email, bool isActive, CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<Exception, CreatedIdentity>>(new CreatedIdentity(Guid.NewGuid().ToString()));
    }

    public Task<Either<Exception, Unit>> Update(string id, Name userName, string email, bool isActive, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, Unit>> Activate(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, Unit>> Deactivate(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, Unit>> Delete(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }
}