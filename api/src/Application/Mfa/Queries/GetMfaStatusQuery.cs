using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Domain.Users;
using MediatR;

namespace Application.Mfa.Queries;

public record GetMfaStatusQuery(UserId UserId) : IRequest<MfaStatusDto>;

public class GetMfaStatusQueryHandler(IUserMfaRepository userMfaRepository)
    : IRequestHandler<GetMfaStatusQuery, MfaStatusDto>
{
    public async Task<MfaStatusDto> Handle(GetMfaStatusQuery request, CancellationToken cancellationToken)
    {
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return userMfaOption.Match(
            mfa => GetMfaStatus(mfa),
            () => new MfaStatusDto(false, null, 0, MfaState.None));
    }

    private static MfaStatusDto GetMfaStatus(UserMfa mfa)
    {
        // User has completed setup if they have a secret
        var hasCompletedSetup = !string.IsNullOrEmpty(mfa.EncryptedSecret);

        if (mfa.IsEnabled)
        {
            return new MfaStatusDto(true, mfa.EnabledAt, mfa.HashedRecoveryCodes.Count, MfaState.Enabled);
        }

        // Record exists but not enabled
        return hasCompletedSetup
            ? new MfaStatusDto(false, null, 0, MfaState.Disabled)
            : new MfaStatusDto(false, null, 0, MfaState.PendingSetup);
    }
}