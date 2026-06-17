using Application.Common.Interfaces.Repositories;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record AdminRequireMfaSetupCommand(UserId TargetUserId) : IRequest<Either<MfaException, bool>>;

public class AdminRequireMfaSetupCommandHandler(
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<AdminRequireMfaSetupCommand, Either<MfaException, bool>>
{
    public async Task<Either<MfaException, bool>> Handle(
        AdminRequireMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => HandleExistingMfa(mfa, request.TargetUserId),
            () => CreateMfaRequirementAsync(request.TargetUserId, cancellationToken));
    }

    private static Task<Either<MfaException, bool>> HandleExistingMfa(UserMfa mfa, UserId userId)
    {
        // If MFA record already exists, check its state
        if (mfa.IsEnabled)
        {
            return Task.FromResult<Either<MfaException, bool>>(new MfaAlreadyEnabledException(userId));
        }

        // Record exists but not enabled - either pending setup or disabled
        // Either way, user already has MFA requirement
        return Task.FromResult<Either<MfaException, bool>>(true);
    }

    private async Task<Either<MfaException, bool>> CreateMfaRequirementAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var userMfa = UserMfa.Create(userId);
        await userMfaRepository.AddAsync(userMfa, cancellationToken);
        return true;
    }
}