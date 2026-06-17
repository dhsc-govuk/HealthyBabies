using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record AdminResetMfaCommand(UserId TargetUserId) : IRequest<Either<MfaException, bool>>;

public class AdminResetMfaCommandHandler(
    IMfaSessionService sessionService,
    IMfaRateLimitService rateLimitService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<AdminResetMfaCommand, Either<MfaException, bool>>
{
    public async Task<Either<MfaException, bool>> Handle(
        AdminResetMfaCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => DeleteMfaAsync(mfa, request.TargetUserId, cancellationToken),
            () => Task.FromResult<Either<MfaException, bool>>(true));
    }

    private async Task<Either<MfaException, bool>> DeleteMfaAsync(
        UserMfa mfa,
        UserId userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await userMfaRepository.DeleteAsync(mfa, cancellationToken);
            await sessionService.RevokeAllUserSessionsAsync(userId);
            rateLimitService.ResetAttempts(userId);
            return true;
        }
        catch (Exception ex)
        {
            return new MfaUnknownException(userId, ex);
        }
    }
}