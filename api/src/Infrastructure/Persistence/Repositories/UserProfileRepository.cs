using Application.Common.Interfaces;
using Domain;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserProfileRepository(ApplicationDbContext dbContext) : IUserProfileRepository, IAsyncDisposable
{
    public async Task<Option<UserProfile>> GetUserProfile(SubId subId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.SubId.Equals(subId), cancellationToken);

        if (user != null)
        {
            return await GetUserProfile(user.Id, user.Role, cancellationToken);
        }

        return Option<UserProfile>.None;
    }

    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }

    private async Task<Option<UserProfile>> GetUserProfile(
        UserId userId,
        UserRole userRole,
        CancellationToken cancellationToken = default)
    {
        if (userRole.Equals(UserRole.Admin))
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id.Equals(userId), cancellationToken);
            if (user != null)
            {
                return UserProfile.NewAdminProfile(user);
            }
        }
        else if (userRole.Equals(UserRole.OrganisationAdmin))
        {
            var user = await dbContext.OrganisationUsers
                .Include(x => x.Organisation)
                .Include(x => x.User)
                .SingleOrDefaultAsync(x => x.UserId.Equals(userId), cancellationToken);
            if (user != null)
            {
                return UserProfile.NewOrganisationAdminProfile(user);
            }
        }

        return Option<UserProfile>.None;
    }
}