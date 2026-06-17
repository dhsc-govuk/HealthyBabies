using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Users.Dtos;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using LanguageExt;

namespace Api.Services.Implementation;

public class OrganisationUsersControllerService(
    IOrganisationUserQueries organisationUserQueries,
    IUserMfaRepository userMfaRepository) : IOrganisationUsersControllerService
{
    public async Task<IEnumerable<OrganisationUserDto>> GetAll(
        Permission permission,
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var users = await organisationUserQueries.GetUsers(
            new OrganisationId(organisationId),
            permission,
            cancellationToken);

        var userIds = users.Select(u => u.User!.Id).ToList();
        var mfaStatuses = await userMfaRepository.GetByUserIdsAsync(userIds, cancellationToken);

        return users.Select(x =>
        {
            var mfaStatus = mfaStatuses.TryGetValue(x.User!.Id, out var mfa)
                ? GetMfaStateLabel(mfa)
                : "Pending Setup";
            return new OrganisationUserDto(
                x.Id.ToString(),
                x.User!.Name.FirstName,
                x.User.Name.LastName,
                x.User.Email,
                x.User.IsActive,
                x.User.Role,
                MfaStatus: mfaStatus);
        });
    }

    private static string GetMfaStateLabel(UserMfa mfa)
    {
        return mfa.IsEnabled ? "Enabled" : "Disabled";
    }

    public async Task<Option<OrganisationUserDto>> Get(
        Permission permission,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var users = await organisationUserQueries.GetOrganisationUserById(
            new OrganisationUserId(userId),
            permission,
            cancellationToken);

        return users.Match(
            u => new OrganisationUserDto(
                u.Id.ToString(),
                u.User!.Name.FirstName,
                u.User.Name.LastName,
                u.User.Email,
                u.User.IsActive,
                u.User.Role),
            () => Option<OrganisationUserDto>.None);
    }
}