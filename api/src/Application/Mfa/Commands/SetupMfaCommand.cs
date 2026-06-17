using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record SetupMfaCommand(UserId UserId, string Email) : IRequest<Either<MfaException, MfaSetupResponseDto>>;

public class SetupMfaCommandHandler(
    ITotpService totpService,
    IMfaEncryptionService encryptionService,
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<SetupMfaCommand, Either<MfaException, MfaSetupResponseDto>>
{
    private const string CacheKeyPrefix = "mfa_setup_pending_";
    private static readonly TimeSpan SetupTimeout = TimeSpan.FromMinutes(5);

    public async Task<Either<MfaException, MfaSetupResponseDto>> Handle(
        SetupMfaCommand request,
        CancellationToken cancellationToken)
    {
        var existingMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return await existingMfaOption.MatchAsync(
            mfa => UpdateExistingSetup(mfa, request, cancellationToken),
            () => CreateNewSetup(request, cancellationToken));
    }

    private async Task<Either<MfaException, MfaSetupResponseDto>> UpdateExistingSetup(
        UserMfa mfa,
        SetupMfaCommand request,
        CancellationToken cancellationToken)
    {
        if (mfa.IsEnabled)
        {
            return new MfaAlreadyEnabledException(request.UserId);
        }

        var secret = totpService.GenerateSecret();
        var qrCodeUri = totpService.GenerateQrCodeUri(request.Email, secret);
        var encryptedSecret = encryptionService.Encrypt(secret);

        mfa.UpdateSecret(encryptedSecret);
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);

        return new MfaSetupResponseDto(qrCodeUri, secret);
    }

    private async Task<Either<MfaException, MfaSetupResponseDto>> CreateNewSetup(
        SetupMfaCommand request,
        CancellationToken cancellationToken)
    {
        var secret = totpService.GenerateSecret();
        var qrCodeUri = totpService.GenerateQrCodeUri(request.Email, secret);

        var cacheKey = $"{CacheKeyPrefix}{request.UserId.Value}";
        var encryptedSecret = encryptionService.Encrypt(secret);

        var userMfa = UserMfa.New(UserMfaId.New(), request.UserId, encryptedSecret);
        await userMfaRepository.AddAsync(userMfa, cancellationToken);

        return new MfaSetupResponseDto(qrCodeUri, secret);
    }
}