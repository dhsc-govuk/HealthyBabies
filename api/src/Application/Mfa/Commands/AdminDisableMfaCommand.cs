using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record AdminDisableMfaCommand(UserId TargetUserId) : IRequest<Either<MfaException, bool>>;

public class AdminDisableMfaCommandHandler(
    IMfaSessionService sessionService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<AdminDisableMfaCommand, Either<MfaException, bool>>
{
    public async Task<Either<MfaException, bool>> Handle(
        AdminDisableMfaCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => ValidateAndDisableAsync(mfa, request.TargetUserId, cancellationToken),
            () => new MfaNotEnabledException(request.TargetUserId));
    }

    private Either<MfaException, UserMfa> ValidateIsEnabled(UserMfa mfa, UserId userId)
    {
        return mfa.IsEnabled
            ? mfa
            : new MfaNotEnabledException(userId);
    }

    private async Task<Either<MfaException, bool>> ValidateAndDisableAsync(
        UserMfa mfa,
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await ValidateIsEnabled(mfa, userId)
            .BindAsync(m => DisableMfaAsync(m, userId, cancellationToken));
    }

    private async Task<Either<MfaException, bool>> DisableMfaAsync(
        UserMfa mfa,
        UserId userId,
        CancellationToken cancellationToken)
    {
        mfa.Disable();
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);
        await sessionService.RevokeAllUserSessionsAsync(userId);
        return true;
    }
}