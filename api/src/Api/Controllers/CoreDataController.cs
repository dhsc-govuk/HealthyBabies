using Api.Services.Abstract;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("admin/core-data")]
[Authorize(Roles = Role.Admin)]
public class CoreDataController(ICoreDataExportService exportService) : ControllerBase
{
    [HttpGet("sites/download")]
    public async Task<ActionResult> DownloadSites(CancellationToken cancellationToken)
    {
        var csvBytes = await exportService.ExportSitesAsCsvAsync(cancellationToken);
        return File(csvBytes, "text/csv", "sites.csv");
    }

    [HttpGet("services/download")]
    public async Task<ActionResult> DownloadServices(CancellationToken cancellationToken)
    {
        var csvBytes = await exportService.ExportServicesAsCsvAsync(cancellationToken);
        return File(csvBytes, "text/csv", "services.csv");
    }
}