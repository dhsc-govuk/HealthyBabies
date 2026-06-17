using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record VerifyMfaCommand(
    UserId UserId,
    string Code,
    string IpAddress,
    string UserAgent) : IRequest<Either<MfaException, MfaVerifyResponseDto>>;

public class VerifyMfaCommandHandler(
    ITotpService totpService,
    IMfaEncryptionService encryptionService,
    IMfaRateLimitService rateLimitService,
    IMfaSessionService sessionService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<VerifyMfaCommand, Either<MfaException, MfaVerifyResponseDto>>
{
    public async Task<Either<MfaException, MfaVerifyResponseDto>> Handle(
        VerifyMfaCommand request,
        CancellationToken cancellationToken)
    {
        if (rateLimitService.IsRateLimited(request.UserId))
        {
            return new MfaRateLimitException(request.UserId);
        }

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
        VerifyMfaCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        return await ValidateIsEnabled(mfa, request.UserId)
            .Bind(m => ValidateNotLockedOut(m, request.UserId))
            .BindAsync(m => ValidateCodeAndCreateSessionAsync(request, m, cancellationToken));
    }

    private async Task<Either<MfaException, MfaVerifyResponseDto>> ValidateCodeAndCreateSessionAsync(
        VerifyMfaCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        var secret = encryptionService.Decrypt(mfa.EncryptedSecret);

        if (!totpService.ValidateCode(secret, request.Code))
        {
            rateLimitService.RecordAttempt(request.UserId);
            mfa.RecordFailedAttempt();
            await userMfaRepository.UpdateAsync(mfa, cancellationToken);
            return new InvalidMfaCodeException(request.UserId);
        }

        rateLimitService.ResetAttempts(request.UserId);
        mfa.ResetFailedAttempts();
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);

        var session = await sessionService.CreateSessionAsync(
            request.UserId,
            request.IpAddress,
            request.UserAgent);

        return new MfaVerifyResponseDto(true, session.Id.Value, session.ExpiresAt);
    }
}