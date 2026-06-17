using Api.Services.Abstract;
using Application.Users.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("admin")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class AdminHomeController(
    IAdminHomeControllerService controllerService)
    : ControllerBase
{
    [HttpGet("totals")]
    public async Task<ActionResult<AdminTotalsResponse>> GetTotals(CancellationToken cancellationToken)
    {
        return await controllerService.GetTotals(cancellationToken);
    }
}