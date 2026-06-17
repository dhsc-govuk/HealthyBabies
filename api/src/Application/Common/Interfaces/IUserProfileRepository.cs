using Domain;
using Domain.Users;
using LanguageExt;

namespace Application.Common.Interfaces;

public interface IUserProfileRepository
{
    Task<Option<UserProfile>> GetUserProfile(SubId subId, CancellationToken cancellationToken = default);
}