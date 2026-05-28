using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Users.Commands.OrganisationUsers;

public record CreateOrganisationUserResult(OrganisationUser OrganisationUser, string? TemporaryPassword);

public record CreateOrganisationUserCommand : IRequest<Either<UserException, CreateOrganisationUserResult>>
{
    public required string? FirstName { get; init; }
    public required string? LastName { get; init; }
    public required string? Email { get; init; }
    public required bool IsActive { get; init; }
    public required Guid OrganisationId { get; init; }
    public required UserRole Role { get; init; }
}

public class CreateOrganisationUserCommandHandler(
    PermissionsService permissionsService,
    IOrganisationRepository organisationRepository,
    IOrganisationUserRepository organisationUsersRepository,
    IUserRepository usersRepository,
    IUsersService usersService)
    : IRequestHandler<CreateOrganisationUserCommand, Either<UserException, CreateOrganisationUserResult>>
{
    public Task<Either<UserException, CreateOrganisationUserResult>> Handle(
      CreateOrganisationUserCommand request,
      CancellationToken cancellationToken)
    {
        var organisationId = new OrganisationId(request.OrganisationId);
        var userParams = new { Email = request.Email!.Trim(), Name = new Name(request.FirstName!, request.LastName!) };
        var permissions = permissionsService.GetOrganisationPermissions();

        return permissions.MatchAsync(
            p => CheckIfUserExists(userParams.Email, cancellationToken)
                .BindAsync(_ => CheckOrganisation(p, organisationId, cancellationToken))
                .BindAsync(_ => GetUserIdentity(request, userParams.Name, userParams.Email, cancellationToken))
                .BindAsync(identity => CreateOrReactivateUser(
                    organisationId,
                    userParams.Name,
                    userParams.Email,
                    identity,
                    request.IsActive,
                    request.Role,
                    cancellationToken)),
            e => new UserArgumentException(UserId.Empty(), e.Message));
    }

    private async Task<Either<UserException, CreatedIdentity>> GetUserIdentity(
        CreateOrganisationUserCommand request,
        Name name,
        string email,
        CancellationToken cancellationToken)
    {
        var identityResult = await CreateUserIdentityIfDoesNotExist(name, email, request.IsActive, cancellationToken);
        return identityResult.Match<Either<UserException, CreatedIdentity>>(
            identity => identity,
            e => new UserUnknownException(UserId.Empty(), e));
    }

    private async Task<Either<UserException, CreateOrganisationUserResult>> CreateOrReactivateUser(
        OrganisationId orgId,
        Name name,
        string email,
        CreatedIdentity identity,
        bool isActive,
        UserRole role,
        CancellationToken cancellationToken)
    {
        try
        {
            var deletedUser = await usersRepository.FindDeletedByEmailAsync(email, cancellationToken);

            return await deletedUser.MatchAsync(
                async existingUser => await ReactivateUser(existingUser, orgId, name, identity, isActive, role, cancellationToken),
                async () => await CreateNewUser(orgId, name, email, identity, isActive, role, cancellationToken));
        }
        catch (Exception exception)
        {
            return new UserUnknownException(UserId.Empty(), exception);
        }
    }

    private async Task<Either<UserException, CreateOrganisationUserResult>> ReactivateUser(
        User existingUser,
        OrganisationId orgId,
        Name name,
        CreatedIdentity identity,
        bool isActive,
        UserRole role,
        CancellationToken cancellationToken)
    {
        existingUser.Reactivate(name, new SubId(Guid.Parse(identity.SubId)), isActive, role);
        await usersRepository.UpdateAsync(existingUser, cancellationToken);

        // Check if an OrganisationUser already exists for this user
        var existingOrgUser = await organisationUsersRepository.FindByUserIdAsync(existingUser.Id, cancellationToken);

        return await existingOrgUser.MatchAsync(
            async existingOrgUser =>
            {
                // Update the existing OrganisationUser
                existingOrgUser.Reactivate(orgId, existingUser);
                var result = await organisationUsersRepository.UpdateAsync(existingOrgUser, cancellationToken);
                return new CreateOrganisationUserResult(result, NullIfEmpty(identity.TemporaryPassword));
            },
            async () =>
            {
                // Create a new OrganisationUser if none exists
                var organisationUserId = new OrganisationUserId(existingUser.Id.Value);
                var organisationUser = OrganisationUser.New(
                    organisationUserId,
                    existingUser.Id,
                    orgId,
                    existingUser);
                var result = await organisationUsersRepository.AddAsync(organisationUser, cancellationToken);
                return new CreateOrganisationUserResult(result, NullIfEmpty(identity.TemporaryPassword));
            });
    }

    private async Task<Either<UserException, CreateOrganisationUserResult>> CreateNewUser(
        OrganisationId orgId,
        Name name,
        string email,
        CreatedIdentity identity,
        bool isActive,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var user = User.New(
            UserId.New(),
            name,
            email,
            new SubId(Guid.Parse(identity.SubId)),
            isActive,
            role);
        var organisationUserId = new OrganisationUserId(user.Id.Value);
        var organisationUser = OrganisationUser.New(
            organisationUserId,
            user.Id,
            orgId,
            user);
        var result = await organisationUsersRepository.AddAsync(organisationUser, cancellationToken);

        return new CreateOrganisationUserResult(result, NullIfEmpty(identity.TemporaryPassword));
    }

    private async Task<Either<UserException, Unit>> CheckOrganisation(
        Permission p,
        OrganisationId id,
        CancellationToken cancellationToken)
    {
        var organisation = await organisationRepository.GetOrganisationById(id, p, cancellationToken);
        return organisation.Match<Either<UserException, Unit>>(
            _ => Unit.Default,
            () => new UserOrganisationDoesNotExistException(UserId.Empty(), id));
    }

    private async Task<Either<UserException, Unit>> CheckIfUserExists(
        string email,
        CancellationToken cancellationToken)
    {
        var existingOrganisationUser = await usersRepository.FindByEmailAsync(email, cancellationToken);

        return existingOrganisationUser.Match<Either<UserException, Unit>>(
            ou => new UserExistsException(ou.Id, email),
            () => Unit.Default);
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