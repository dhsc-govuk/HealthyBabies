using Application.Common.Interfaces.Repositories;
using Domain.Users;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserMfaRepository(ApplicationDbContext dbContext) : IUserMfaRepository
{
    public async Task<Option<UserMfa>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.UserMfas
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return result == null ? Option<UserMfa>.None : Option<UserMfa>.Some(result);
    }

    public async Task<Dictionary<UserId, UserMfa>> GetByUserIdsAsync(IEnumerable<UserId> userIds, CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.ToList();
        var results = await dbContext.UserMfas
            .Where(x => userIdList.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        return results.ToDictionary(x => x.UserId, x => x);
    }

    public async Task<UserMfa> AddAsync(UserMfa userMfa, CancellationToken cancellationToken = default)
    {
        await dbContext.UserMfas.AddAsync(userMfa, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return userMfa;
    }

    public async Task<UserMfa> UpdateAsync(UserMfa userMfa, CancellationToken cancellationToken = default)
    {
        dbContext.UserMfas.Update(userMfa);
        await dbContext.SaveChangesAsync(cancellationToken);
        return userMfa;
    }

    public async Task DeleteAsync(UserMfa userMfa, CancellationToken cancellationToken = default)
    {
        dbContext.UserMfas.Remove(userMfa);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}