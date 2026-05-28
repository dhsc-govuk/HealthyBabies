using Api.Services.Abstract;
using Application.Common.Interfaces;
using Application.Users.Dtos;
using LanguageExt;

namespace Api.Services.Implementation;

public class ProfileControllerService(
    IIdentityService identityService,
    IInMemoryCache inMemoryCache) : IProfileControllerService
{
    public Option<Profile> GetProfile()
    {
        var subId = identityService.GetSubId();
        return subId.Match(
            s => inMemoryCache.GetItem<Profile>($"user::{s}").Match(
                cachedUser => cachedUser,
                () => Option<Profile>.None),
            () => Option<Profile>.None);
    }
}