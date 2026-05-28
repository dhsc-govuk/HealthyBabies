using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Users.Commands.Admins;

public record DeleteAdminCommand : IRequest<Either<UserException, User>>
{
    public Guid Id { get; init; }
}

public class DeleteAdminCommandHandler(
    PermissionsService permissionsService,
    IUserRepository repository,
    IUsersService usersService)
    : IRequestHandler<DeleteAdminCommand, Either<UserException, User>>
{
    public async Task<Either<UserException, User>> Handle(
        DeleteAdminCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.Id);
        var deletedByResult = permissionsService.GetUserId();

        return await deletedByResult.MatchAsync(
            deletedById => GetAndDeleteUser(userId, deletedById, cancellationToken),
            e => new UserArgumentException(userId, e.Message));
    }

    private async Task<Either<UserException, User>> GetAndDeleteUser(
        UserId userId,
        UserId deletedById,
        CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(userId, cancellationToken);
        return await user.MatchAsync(
            u => DeleteUser(u, deletedById, cancellationToken),
            () => new UserDoesNotExistException(userId));
    }

    private async Task<Either<UserException, User>> DeleteUser(
        User user,
        UserId deletedById,
        CancellationToken cancellationToken)
    {
        user.Delete(deletedById);

        var identityResult = await usersService.Delete(user.SubId.Value.ToString(), cancellationToken);

        return await identityResult.MatchAsync<Either<UserException, User>>(
            async _ =>
            {
                await repository.UpdateAsync(user, cancellationToken);
                return user;
            },
            e => new UserUnknownException(user.Id, e));
    }
}