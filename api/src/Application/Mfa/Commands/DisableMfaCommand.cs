using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record DisableMfaCommand(UserId UserId, string Code) : IRequest<Either<MfaException, bool>>;

public class DisableMfaCommandHandler(
    ITotpService totpService,
    IMfaEncryptionService encryptionService,
    IMfaSessionService sessionService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<DisableMfaCommand, Either<MfaException, bool>>
{
    public async Task<Either<MfaException, bool>> Handle(
        DisableMfaCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => ValidateAndDisableAsync(request, mfa, cancellationToken),
            () => new MfaNotEnabledException(request.UserId));
    }

    private Either<MfaException, UserMfa> ValidateIsEnabled(UserMfa mfa, UserId userId)
    {
        return mfa.IsEnabled
            ? mfa
            : new MfaNotEnabledException(userId);
    }

    private Either<MfaException, UserMfa> ValidateCode(UserMfa mfa, string code, UserId userId)
    {
        var secret = encryptionService.Decrypt(mfa.EncryptedSecret);
        return totpService.ValidateCode(secret, code)
            ? mfa
            : new InvalidMfaCodeException(userId);
    }

    private async Task<Either<MfaException, bool>> ValidateAndDisableAsync(
        DisableMfaCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        return await ValidateIsEnabled(mfa, request.UserId)
            .Bind(m => ValidateCode(m, request.Code, request.UserId))
            .BindAsync(m => DisableMfaAsync(m, request.UserId, cancellationToken));
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