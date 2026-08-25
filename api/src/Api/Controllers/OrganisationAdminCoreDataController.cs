using Api.Services.Abstract;
using Application.Common.Permissions;
using Domain.Organisations;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// BLUM-7251: LA self-serve equivalent of CoreDataController (admin/core-data) - the organisation
// comes from the caller's own permission, never a route/query parameter, so an LA can only ever
// download its own sites/services data.
[ApiController]
[Route("organisation-admin/core-data")]
[Authorize(Roles = Role.OrganisationAdmin)]
public class OrganisationAdminCoreDataController(
    PermissionsService permissionsService,
    ICoreDataExportService exportService)
    : ControllerBase
{
    [HttpGet("sites/download")]
    public async Task<ActionResult> DownloadSites(CancellationToken cancellationToken)
    {
        return await ExportAsync(
            (orgId, ct) => exportService.ExportSitesAsCsvAsync(orgId, ct),
            "sites.csv",
            cancellationToken);
    }

    [HttpGet("services/download")]
    public async Task<ActionResult> DownloadServices(CancellationToken cancellationToken)
    {
        return await ExportAsync(
            (orgId, ct) => exportService.ExportServicesAsCsvAsync(orgId, ct),
            "services.csv",
            cancellationToken);
    }

    private async Task<ActionResult> ExportAsync(
        Func<OrganisationId, CancellationToken, Task<byte[]>> export,
        string fileName,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId,
                    () => (OrganisationId?)null);

                if (organisationId == null)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var csvBytes = await export(organisationId, cancellationToken);
                return File(csvBytes, "text/csv", fileName);
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
    }
}