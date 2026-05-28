using Application.Common.Interfaces.Repositories;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Users.Commands.Admins;

public record UpdateAdminCommand : IRequest<Either<UserException, User>>
{
    public Guid Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateAdminCommandHandler(IUserRepository repository, IUsersService usersService)
    : IRequestHandler<UpdateAdminCommand, Either<UserException, User>>
{
    public async Task<Either<UserException, User>> Handle(
        UpdateAdminCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.Id);
        var userResult = await GetUser(userId, cancellationToken);
        return await userResult.BindAsync(user =>
            UpdateUser(request, user, cancellationToken));
    }

    private async Task<Either<UserException, User>> GetUser(
        UserId id,
        CancellationToken cancellationToken)
    {
        var userById = await repository.FindById(id, cancellationToken);
        return userById.Match<Either<UserException, User>>(
            user => user.Role.Equals(UserRole.Admin)
                ? user
                : new UserWrongRoleException(user.Id, UserRole.Admin),
            () => new UserDoesNotExistException(id));
    }

    private async Task<Either<UserException, User>> UpdateUser(UpdateAdminCommand request, User user, CancellationToken cancellationToken)
    {
        var userParams = new { Email = request.Email!.Trim(), Name = new Name(request.FirstName!, request.LastName!) };

        var userWithEmail = await repository.FindDuplicateAsync(userParams.Email, user.Id, cancellationToken);
        return await userWithEmail.MatchAsync(
            u => new UserExistsException(u.Id, u.Email),
            () => UpdateUserDetails(user, userParams.Name, userParams.Email, request.IsActive, cancellationToken));
    }

    private async Task<Either<UserException, User>> UpdateUserDetails(User user, Name name, string email, bool isActive, CancellationToken cancellationToken)
    {
        user.UpdateDetails(
            name,
            email,
            isActive);

        var result = await usersService.Update(
            user.SubId.Value.ToString(),
            user.Name,
            user.Email,
            user.IsActive);

        return await result.MatchAsync<Either<UserException, User>>(
            async _ => await repository.UpdateAsync(user, cancellationToken),
            exception => new UserUnknownException(user.Id, exception));
    }
}