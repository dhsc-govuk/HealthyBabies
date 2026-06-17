using Application.Common.Interfaces.Repositories;
using Application.Mfa.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Mfa.Commands;

public record AdminEnableMfaCommand(UserId TargetUserId) : IRequest<Either<MfaException, bool>>;

public class AdminEnableMfaCommandHandler(
    IUserMfaRepository userMfaRepository)
    : IRequestHandler<AdminEnableMfaCommand, Either<MfaException, bool>>
{
    public async Task<Either<MfaException, bool>> Handle(
        AdminEnableMfaCommand request,
        CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);

        return await userMfaOption.MatchAsync(
            mfa => EnableMfaAsync(mfa, request.TargetUserId, cancellationToken),
            () => new MfaNotSetupException(request.TargetUserId));
    }

    private async Task<Either<MfaException, bool>> EnableMfaAsync(
        UserMfa mfa,
        UserId userId,
        CancellationToken cancellationToken)
    {
        // Can only re-enable if user has completed setup (has a secret)
        if (string.IsNullOrEmpty(mfa.EncryptedSecret))
        {
            return new MfaNotSetupException(userId);
        }

        if (mfa.IsEnabled)
        {
            return new MfaAlreadyEnabledException(userId);
        }

        mfa.Enable();
        await userMfaRepository.UpdateAsync(mfa, cancellationToken);
        return true;
    }
}