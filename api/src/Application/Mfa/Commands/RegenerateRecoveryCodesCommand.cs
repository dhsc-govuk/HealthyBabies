using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record RegenerateRecoveryCodesCommand(UserId UserId, string Code) : IRequest<Either<MfaException, MfaRecoveryCodesDto>>;

public class RegenerateRecoveryCodesCommandHandler(
    ITotpService totpService,
    IMfaEncryptionService encryptionService,
    IRecoveryCodeService recoveryCodeService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<RegenerateRecoveryCodesCommand, Either<MfaException, MfaRecoveryCodesDto>>
{
    public async Task<Either<MfaException, MfaRecoveryCodesDto>> Handle(
        RegenerateRecoveryCodesCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => ValidateAndRegenerateAsync(request, mfa, cancellationToken),
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

    private async Task<Either<MfaException, MfaRecoveryCodesDto>> ValidateAndRegenerateAsync(
        RegenerateRecoveryCodesCommand request,
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        return await ValidateIsEnabled(mfa, request.UserId)
            .Bind(m => ValidateCode(m, request.Code, request.UserId))
            .BindAsync(m => RegenerateCodesAsync(m, cancellationToken));
    }

    private async Task<Either<MfaException, MfaRecoveryCodesDto>> RegenerateCodesAsync(
        UserMfa mfa,
        CancellationToken cancellationToken)
    {
        var recoveryCodes = recoveryCodeService.GenerateRecoveryCodes();
        var hashedCodes = recoveryCodes.Select(recoveryCodeService.HashRecoveryCode).ToList();

        mfa.UpdateRecoveryCodes(hashedCodes);
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);

        return new MfaRecoveryCodesDto(recoveryCodes);
    }
}