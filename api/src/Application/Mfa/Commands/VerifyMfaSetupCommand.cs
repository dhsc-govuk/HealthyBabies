using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record VerifyMfaSetupCommand(UserId UserId, string Code, string IpAddress, string UserAgent) : IRequest<Either<MfaException, MfaSetupCompleteResponseDto>>;

public class VerifyMfaSetupCommandHandler(
    ITotpService totpService,
    IMfaEncryptionService encryptionService,
    IRecoveryCodeService recoveryCodeService,
    IUserMfaRepository userMfaRepository,
    IMfaSessionService sessionService)
    : IRequestHandler<VerifyMfaSetupCommand, Either<MfaException, MfaSetupCompleteResponseDto>>
{
    public async Task<Either<MfaException, MfaSetupCompleteResponseDto>> Handle(
        VerifyMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => ValidateAndCompleteSetupAsync(mfa, request, cancellationToken),
            () => Task.FromResult<Either<MfaException, MfaSetupCompleteResponseDto>>(
                new MfaSetupExpiredException(request.UserId)));
    }

    private async Task<Either<MfaException, MfaSetupCompleteResponseDto>> ValidateAndCompleteSetupAsync(
        UserMfa mfa,
        VerifyMfaSetupCommand request,
        CancellationToken cancellationToken)
    {
        if (mfa.IsEnabled)
        {
            return new MfaAlreadyEnabledException(request.UserId);
        }

        if (string.IsNullOrEmpty(mfa.EncryptedSecret))
        {
            return new MfaSetupExpiredException(request.UserId);
        }

        var secret = encryptionService.Decrypt(mfa.EncryptedSecret);
        if (!totpService.ValidateCode(secret, request.Code))
        {
            return new InvalidMfaCodeException(request.UserId);
        }

        var recoveryCodes = recoveryCodeService.GenerateRecoveryCodes();
        var hashedCodes = recoveryCodes.Select(recoveryCodeService.HashRecoveryCode).ToList();

        mfa.Enable(hashedCodes);
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);

        var session = await sessionService.CreateSessionAsync(
            request.UserId,
            request.IpAddress,
            request.UserAgent);

        return new MfaSetupCompleteResponseDto(true, recoveryCodes, session.Id.Value, session.ExpiresAt);
    }
}