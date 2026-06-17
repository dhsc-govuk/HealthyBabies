using Application.Common.Interfaces.Repositories;
using Application.Users.Exceptions;
using Application.Users.Interfaces;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Users.Commands.Admins;

public class ActivateOrDeactivateUserCommand : IRequest<Either<UserException, User>>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class ActivateOrDeactivateUserCommandHandler(IUserRepository userRepository, IUsersService usersService) : IRequestHandler<ActivateOrDeactivateUserCommand, Either<UserException, User>>
{
    public async Task<Either<UserException, User>> Handle(ActivateOrDeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var enitityId = new UserId(request.Id);
        var userResult = await GetUser(enitityId, cancellationToken);
        return await userResult.BindAsync(user => UpdateUserDetails(user, request.IsActive, cancellationToken));
    }

    private async Task<Either<UserException, User>> GetUser(UserId id, CancellationToken cancellationToken)
    {
        var userById = await userRepository.GetByIdAsync(id, cancellationToken);
        return userById.Match<Either<UserException, User>>(
            user => user,
            () => new UserDoesNotExistException(id));
    }

    private async Task<Either<UserException, User>> UpdateUserDetails(User user, bool isActive, CancellationToken cancellationToken)
    {
        user.UpdateDetails(
            user.Name,
            user.Email,
            isActive);

        var result = await usersService.Update(
            user.SubId.Value.ToString(),
            user.Name,
            user.Email,
            user.IsActive);

        return await result.MatchAsync<Either<UserException, User>>(
            async _ => await userRepository.UpdateAsync(user, cancellationToken),
            exception => new UserUnknownException(user.Id, exception));
    }
}