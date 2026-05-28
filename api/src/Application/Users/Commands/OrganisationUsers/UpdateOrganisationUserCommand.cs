using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Users.Commands.OrganisationUsers;

public record UpdateOrganisationUserCommand : IRequest<Either<UserException, OrganisationUser>>
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required bool IsActive { get; init; }
    public required UserRole Role { get; init; }
}

public class UpdateOrganisationUserCommandHandler(
    PermissionsService permissionsService,
    IOrganisationUserRepository organisationUserRepository,
    IUserRepository usersRepository,
    IUsersService usersService)
    : IRequestHandler<UpdateOrganisationUserCommand, Either<UserException, OrganisationUser>>
{
    public Task<Either<UserException, OrganisationUser>> Handle(
        UpdateOrganisationUserCommand request,
        CancellationToken cancellationToken)
    {
        var organisationUserId = new OrganisationUserId(request.Id);

        var userParams = new { Email = request.Email.Trim(), Name = new Name(request.FirstName, request.LastName) };

        var permissions = permissionsService.GetOrganisationPermissions();
        return permissions.MatchAsync(
            p => GetOrganisationUserById(p, organisationUserId, cancellationToken)
                .BindAsync(orgUser => FindDuplicate(userParams.Email, orgUser, cancellationToken)
                .BindAsync(_ => UpdateUserDetails(
                    orgUser.User!,
                    userParams.Name,
                    userParams.Email,
                    request.IsActive,
                    request.Role)
                .BindAsync(_ => UpdateUserEntity(request, orgUser, cancellationToken)))),
            e => new UserArgumentException(UserId.Empty(), e.Message));
    }

    private async Task<Either<UserException, Unit>> FindDuplicate(
        string email,
        OrganisationUser user,
        CancellationToken cancellationToken)
    {
        var userWithEmail = await usersRepository.FindDuplicateAsync(email, user.UserId, cancellationToken);
        return userWithEmail.Match<Either<UserException, Unit>>(
            u => new UserExistsException(u.Id, u.Email),
            () => Unit.Default);
    }

    private async Task<Either<UserException, Unit>> UpdateUserDetails(
        User user,
        Name name,
        string email,
        bool isActive,
        UserRole role)
    {
        user.UpdateDetails(
            name,
            email,
            isActive,
            role);
        var result = await usersService.Update(
            user.SubId.Value.ToString(),
            user.Name,
            user.Email,
            user.IsActive);

        return result.Match<Either<UserException, Unit>>(
            _ => Unit.Default,
            e => new UserUnknownException(user.Id, e));
    }

    private async Task<Either<UserException, OrganisationUser>> UpdateUserEntity(
        UpdateOrganisationUserCommand request,
        OrganisationUser organisationUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await usersRepository.UpdateAsync(organisationUser.User!, cancellationToken);
            return await organisationUserRepository.UpdateAsync(organisationUser, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UserUnknownException(organisationUser.UserId, exception);
        }
    }

    private async Task<Either<UserException, OrganisationUser>> GetOrganisationUserById(
        Permission p,
        OrganisationUserId id,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(id.Value);
        var user = await organisationUserRepository.GetOrganisationUserById(id, p, cancellationToken);
        return user.Match<Either<UserException, OrganisationUser>>(
            u => u,
            () => new UserDoesNotExistException(userId));
    }
}