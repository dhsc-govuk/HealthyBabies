using Api.Services.Abstract;
using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Mfa.Dtos;
using Application.Users.Dtos;
using Domain.Users;
using LanguageExt;

namespace Api.Services.Implementation;

public class AdminUsersControllerService(
    IUserQueries userQueries,
    IUserMfaRepository userMfaRepository) : IAdminUsersControllerService
{
    public async Task<IEnumerable<AdminUserDto>> GetAll(CancellationToken cancellationToken)
    {
        var users = await userQueries.GetAdminUsers(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();
        var mfaStatuses = await userMfaRepository.GetByUserIdsAsync(userIds, cancellationToken);

        return users.Select(x =>
        {
            var mfaStatus = mfaStatuses.TryGetValue(x.Id, out var mfa)
                ? GetMfaStateLabel(mfa)
                : "Pending Setup";
            return new AdminUserDto(x.Id.ToString(), x.Name.FirstName, x.Name.LastName, x.Email, x.IsActive, mfaStatus);
        });
    }

    private static string GetMfaStateLabel(UserMfa mfa)
    {
        return mfa.IsEnabled ? "Enabled" : !string.IsNullOrEmpty(mfa.EncryptedSecret) ? "Disabled" : "Disabled";
    }

    public async Task<Option<AdminUserDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var entityId = new UserId(userId);
        var user = await userQueries.FindById(entityId, cancellationToken);

        return user.Match(
            u => new AdminUserDto(
                u.Id.ToString(),
                u.Name.FirstName,
                u.Name.LastName,
                u.Email,
                u.IsActive),
            () => Option<AdminUserDto>.None);
    }

    public async Task<PaginatedResult<OrganisationUserDto>> GetOrganisationUsers(OrganisationUserQueryDto query, CancellationToken cancellationToken)
    {
        var users = await userQueries.GetOrganisationUser(query.OrganisationId, query.PageSize, query.PageNumber, cancellationToken);

        return new PaginatedResult<OrganisationUserDto>
        {
            Items = users.Items.Select(x => new OrganisationUserDto(
                x.User!.Id.ToString(),
                x.User!.Name.FirstName,
                x.User!.Name.LastName,
                x.User!.Email,
                x.User.IsActive,
                x.User.Role.ToString(),
                x.Organisation?.Name,
                x.OrganisationId?.ToString())).ToList(),
            CurrentPage = users.CurrentPage,
            PageSize = users.PageSize,
            TotalCount = users.TotalCount,
            TotalPages = users.TotalPages
        };
    }

    public async Task<Option<OrganisationUserDto>> GetOrganisationUser(Guid userId, CancellationToken cancellationToken)
    {
        var entityId = new UserId(userId);
        var user = await userQueries.GetOrganisationUserById(entityId, cancellationToken);

        return user.Match(
            u => new OrganisationUserDto(
                u.User!.Id.ToString(),
                u.User!.Name.FirstName,
                u.User!.Name.LastName,
                u.User!.Email,
                u.User.IsActive,
                u.User.Role.ToString(),
                u.Organisation?.Name,
                u.OrganisationId?.ToString()),
            () => Option<OrganisationUserDto>.None);
    }

    public async Task<MfaStatusDto> GetMfaStatus(Guid userId, CancellationToken cancellationToken)
    {
        var entityId = new UserId(userId);
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(entityId, cancellationToken);

        return userMfaOption.Match(
            mfa => GetMfaStatus(mfa),
            () => new MfaStatusDto(false, null, 0, MfaState.None));
    }

    private static MfaStatusDto GetMfaStatus(UserMfa mfa)
    {
        var hasCompletedSetup = !string.IsNullOrEmpty(mfa.EncryptedSecret);

        if (mfa.IsEnabled)
        {
            return new MfaStatusDto(true, mfa.EnabledAt, mfa.HashedRecoveryCodes.Count, MfaState.Enabled);
        }

        return hasCompletedSetup
            ? new MfaStatusDto(false, null, 0, MfaState.Disabled)
            : new MfaStatusDto(false, null, 0, MfaState.PendingSetup);
    }
}