using Api.Services.Abstract;
using Application.Common.Permissions;
using Application.Organisations.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("organisation-admin")]
[ApiController]
[Authorize(Roles = Role.OrganisationAdmin)]
public class OrganisationManagerHomeController(
    PermissionsService permissionsService,
    IOrganisationManagerHomeControllerService controllerService)
    : ControllerBase
{
    [HttpGet("totals")]
    public async Task<ActionResult<OrganisationAdminHomeDto>> GetTotals(CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<OrganisationAdminHomeDto>>(
            async p => await controllerService.GetTotals(p, cancellationToken),
            e => Unauthorized(e));
    }
}