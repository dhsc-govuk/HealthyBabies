using System.Text.Json;
using Infrastructure.Persistence.Seeders;
using Infrastructure.Persistence.Seeders.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("seeder-data")]
[ApiController]
[AllowAnonymous]
public class SeederDataController(
    ISeederDataService seederDataService,
    ISeederDataImporter seederDataImporter) : ControllerBase
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [HttpPost("export")]
    public async Task<ActionResult<SeederExportResultDto>> ExportSeederData(CancellationToken cancellationToken)
    {
        var filePath = await seederDataService.ExportSeederDataAsync(cancellationToken);
        return Ok(new SeederExportResultDto(filePath, DateTime.UtcNow));
    }

    [HttpGet("status")]
    public async Task<ActionResult<SeederDataStatusDto>> GetSeederDataStatus(CancellationToken cancellationToken)
    {
        var data = await seederDataService.LoadSeederDataAsync(cancellationToken);

        if (data == null)
        {
            return Ok(new SeederDataStatusDto(
                false,
                null,
                null,
                "No seeder data file found"));
        }

        return Ok(new SeederDataStatusDto(
            true,
            data.ExportedAt,
            data.Environment,
            $"Seeder data available: {data.ServiceFormQuestions.Count} service form questions, " +
            $"{data.SiteFormQuestions.Count} site form questions, " +
            $"{data.ServiceCategoryFormQuestions.Count} service category form questions, " +
            $"{data.WiderServiceCategories.Count} wider service categories, " +
            $"{data.DataCollectionFormModules.Count} data collection form modules"));
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadSeederData(CancellationToken cancellationToken)
    {
        var jsonContent = await seederDataService.GetSeederDataJsonAsync(cancellationToken);

        if (jsonContent == null)
        {
            return NotFound("No seeder data file found. Please export first.");
        }

        var fileName = $"seeder-data-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);

        return File(bytes, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<ActionResult<SeederImportResult>> ImportSeederData(
        [FromQuery] bool flushExisting = true,
        CancellationToken cancellationToken = default)
    {
        var result = await seederDataImporter.ImportManuallyAsync(flushExisting, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<SeederImportResult>> UploadAndImportSeederData(
        IFormFile file,
        [FromQuery] bool flushExisting = true,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new SeederImportResult(false, "No file uploaded", 0, 0, 0, 0, 0));
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new SeederImportResult(false, "File must be a JSON file", 0, 0, 0, 0, 0));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var jsonContent = await reader.ReadToEndAsync(cancellationToken);

        var result = await seederDataImporter.ImportFromJsonAsync(jsonContent, flushExisting, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("sync-file")]
    public async Task<ActionResult<SeederImportResult>> SyncSeederDataFromFile(
        CancellationToken cancellationToken = default)
    {
        var result = await seederDataImporter.SyncManuallyAsync(cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SeederImportResult>> SyncSeederData(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new SeederImportResult(false, "No file uploaded", 0, 0, 0, 0, 0));
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new SeederImportResult(false, "File must be a JSON file", 0, 0, 0, 0, 0));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var jsonContent = await reader.ReadToEndAsync(cancellationToken);

        var result = await seederDataImporter.SyncFromJsonAsync(jsonContent, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("upload-json")]
    public async Task<ActionResult<SeederImportResult>> UploadJsonSeederData(
        [FromBody] SeederExportData data,
        [FromQuery] bool flushExisting = true,
        CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(data, CamelCaseOptions);
        var result = await seederDataImporter.ImportFromJsonAsync(jsonContent, flushExisting, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("sync-json")]
    public async Task<ActionResult<SeederImportResult>> SyncJsonSeederData(
        [FromBody] SeederExportData data,
        CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(data, CamelCaseOptions);
        var result = await seederDataImporter.SyncFromJsonAsync(jsonContent, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public record SeederExportResultDto(string FilePath, DateTime ExportedAt);

public record SeederDataStatusDto(
    bool HasSeederData,
    DateTime? ExportedAt,
    string? SourceEnvironment,
    string Message);