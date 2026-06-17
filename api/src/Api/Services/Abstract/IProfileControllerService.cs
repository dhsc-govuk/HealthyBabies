using Application.Users.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IProfileControllerService
{
    Option<Profile> GetProfile();
}