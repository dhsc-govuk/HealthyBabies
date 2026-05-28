using Application.Common.Interfaces.Repositories;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Users.Commands.Admins;

public record CreateAdminResult(User User, string? TemporaryPassword);

public record CreateAdminCommand : IRequest<Either<UserException, CreateAdminResult>>
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
}

public class CreateAdminCommandHandler(IUserRepository repository, IUsersService usersService)
    : IRequestHandler<CreateAdminCommand, Either<UserException, CreateAdminResult>>
{
    public async Task<Either<UserException, CreateAdminResult>> Handle(
        CreateAdminCommand request,
        CancellationToken cancellationToken)
    {
        var userParams = new { Email = request.Email!.Trim(), Name = new Name(request.FirstName!, request.LastName!) };

        // Check if active user with this email exists
        var userWithEmail = await repository.FindByEmailAsync(userParams.Email, cancellationToken);
        return await userWithEmail.MatchAsync(
            user => new UserExistsException(user.Id, user.Email),
            () => CheckForDeletedUserAndCreate(userParams.Name, userParams.Email, request.IsActive, cancellationToken));
    }

    private async Task<Either<UserException, CreateAdminResult>> CheckForDeletedUserAndCreate(
        Name name,
        string email,
        bool isActive,
        CancellationToken cancellationToken)
    {
        // Check if a soft-deleted user with this email exists
        var deletedUser = await repository.FindDeletedByEmailAsync(email, cancellationToken);
        return await deletedUser.MatchAsync(
            user => ReactivateUser(user, name, isActive, cancellationToken),
            () => CreateUser(name, email, isActive, cancellationToken));
    }

    private async Task<Either<UserException, CreateAdminResult>> ReactivateUser(
        User user,
        Name name,
        bool isActive,
        CancellationToken cancellationToken)
    {
        // Reactivate the soft-deleted user. If the Entra user no longer exists, fall
        // back to creating a fresh Entra identity and rebinding the local row to the new SubId.
        var identityResult = await usersService.Activate(user.SubId.Value.ToString(), cancellationToken);
        return await identityResult.MatchAsync<Either<UserException, CreateAdminResult>>(
            _ => FinaliseReactivation(user, name, isActive, user.SubId, temporaryPassword: null, cancellationToken),
            async _ =>
            {
                var fallback = await CreateUserIdentityIfDoesNotExist(name, user.Email, isActive, cancellationToken);
                return await fallback.MatchAsync(
                    identity => FinaliseReactivation(user, name, isActive, new SubId(Guid.Parse(identity.SubId)), NullIfEmpty(identity.TemporaryPassword), cancellationToken),
                    ex => new UserUnknownException(user.Id, ex));
            });
    }

    private async Task<Either<UserException, CreateAdminResult>> FinaliseReactivation(
        User user,
        Name name,
        bool isActive,
        SubId subId,
        string? temporaryPassword,
        CancellationToken cancellationToken)
    {
        var trackedUser = await repository.GetByIdIgnoringFiltersAsync(user.Id, cancellationToken);
        if (trackedUser.IsNone)
        {
            return new UserDoesNotExistException(user.Id);
        }

        var u = trackedUser.Match(x => x, () => throw new InvalidOperationException());
        u.Reactivate(name, subId, isActive, UserRole.Admin);
        await repository.UpdateAsync(u, cancellationToken);
        return new CreateAdminResult(u, temporaryPassword);
    }

    private async Task<Either<UserException, CreateAdminResult>> CreateUser(Name name, string email, bool isActive, CancellationToken cancellationToken)
    {
        var identityResult = await CreateUserIdentityIfDoesNotExist(name, email, isActive, cancellationToken);
        return await identityResult.MatchAsync(
            identity => CreateUser(name, email, identity.SubId, NullIfEmpty(identity.TemporaryPassword), isActive, cancellationToken),
            e => new UserUnknownException(UserId.Empty(), e));
    }

    private async Task<Either<UserException, CreateAdminResult>> CreateUser(
        Name name,
        string email,
        string subId,
        string? temporaryPassword,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = User.New(
                UserId.New(),
                name,
                email,
                new SubId(Guid.Parse(subId)),
                isActive,
                UserRole.Admin);

            var createdUser = await repository.AddAsync(user, cancellationToken);

            return new CreateAdminResult(createdUser, temporaryPassword);
        }
        catch (Exception exception)
        {
            return new UserUnknownException(UserId.Empty(), exception);
        }
    }

    private async Task<Either<Exception, CreatedIdentity>> CreateUserIdentityIfDoesNotExist(
        Name userName,
        string email,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var existingUserSubId = await usersService.FindByEmail(email, cancellationToken);
        return await existingUserSubId.MatchAsync(
            subId => new CreatedIdentity(subId, string.Empty),
            () => usersService.AddWithPassword(userName, email, isActive, cancellationToken));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}