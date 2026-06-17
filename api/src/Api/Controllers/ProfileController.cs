using Api.Services.Abstract;
using Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("profile")]
[ApiController]
[Authorize]
public class ProfileController(IProfileControllerService controllerService)
    : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<Profile> GetMe()
    {
        var result = controllerService.GetProfile();

        return result.Match<ActionResult<Profile>>(
            p => p,
            () => NotFound());
    }
}