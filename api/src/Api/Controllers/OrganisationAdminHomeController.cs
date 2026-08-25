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
    IOrganisationManagerHomeControllerService controllerService,
    IOrganisationAnalyticsControllerService analyticsService)
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

    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary(
        [FromQuery] string yearRange,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yearRange))
        {
            return BadRequest("yearRange is required.");
        }

        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<FinancialSummaryDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                return await analyticsService.GetFinancialSummary(organisationId, yearRange, cancellationToken);
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("delivery-summary")]
    public async Task<ActionResult<DeliverySummaryDto>> GetDeliverySummary(
        [FromQuery] string? strand,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<DeliverySummaryDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                return await analyticsService.GetDeliverySummary(organisationId, strand, cancellationToken);
            },
            e => Unauthorized(e.Message));
    }
}