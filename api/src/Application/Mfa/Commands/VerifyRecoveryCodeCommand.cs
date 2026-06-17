using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record VerifyRecoveryCodeCommand(
    UserId UserId,
    string RecoveryCode,
    string IpAddress,
    string UserAgent) : IRequest<Either<MfaException, MfaVerifyResponseDto>>;

public class VerifyRecoveryCodeCommandHandler(
    IRecoveryCodeService recoveryCodeService,
    IMfaSessionService sessionService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<VerifyRecoveryCodeCommand, Either<MfaException, MfaVerifyResponseDto>>
{
    public async Task<Either<MfaException, MfaVerifyResponseDto>> Handle(
        VerifyRecoveryCodeCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => ValidateAndVerifyAsync(request, mfa, cancellationToken),
            () => new MfaNotEnabledException(request.UserId));
    }

    private Either<MfaException, UserMfa> ValidateIsEnabled(UserMfa mfa, UserId userId)
    {
        return mfa.IsEnabled
            ? mfa
            : new MfaNotEnabledException(userId);
    }

    private Either<MfaException, UserMfa> ValidateNotLockedOut(UserMfa mfa, UserId userId)
    {
        return mfa.IsLockedOut()
            ? new MfaRateLimitException(userId, mfa.LockedUntil)
            : mfa;
    }

    private async Task<Either<MfaException, MfaVerifyResponseDto>> ValidateAndVerifyAsync(
        VerifyRecoveryCodeCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        return await ValidateIsEnabled(mfa, request.UserId)
            .Bind(m => ValidateNotLockedOut(m, request.UserId))
            .BindAsync(m => ValidateRecoveryCodeAndCreateSessionAsync(request, m, cancellationToken));
    }

    private async Task<Either<MfaException, MfaVerifyResponseDto>> ValidateRecoveryCodeAndCreateSessionAsync(
        VerifyRecoveryCodeCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        string? matchedHashedCode = null;
        foreach (var hashedCode in mfa.HashedRecoveryCodes)
        {
            if (recoveryCodeService.VerifyRecoveryCode(request.RecoveryCode, hashedCode))
            {
                matchedHashedCode = hashedCode;
                break;
            }
        }

        if (matchedHashedCode == null)
        {
            mfa.RecordFailedAttempt();
            await userMfaRepository.UpdateAsync(mfa, cancellationToken);
            return new InvalidRecoveryCodeException(request.UserId);
        }

        mfa.RemoveRecoveryCode(matchedHashedCode);
        mfa.ResetFailedAttempts();
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);

        var session = await sessionService.CreateSessionAsync(
            request.UserId,
            request.IpAddress,
            request.UserAgent);

        return new MfaVerifyResponseDto(true, session.Id.Value, session.ExpiresAt);
    }
}