using Application.Common.FileValidation;
using Application.Submissions.Commands;
using Application.Submissions.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// BLUM-7454: one-off admin endpoint for importing historical Delivery Plan activity data on behalf
// of a local authority (pilot: Manchester, Birmingham, York). No frontend calls this - it's driven
// directly, by the migration tooling, for the pilot and the eventual live rollout.
[Route("admin/delivery-plan/migrations")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DeliveryPlanMigrationsController(
    IMediator mediator,
    IFileValidationService fileValidationService)
    : ControllerBase
{
    [HttpPost("activities")]
    public async Task<ActionResult<MigrationImportResultDto>> ImportActivities(
        [FromForm] string organisationName,
        [FromForm] Guid dataCollectionId,
        [FromForm] Guid moduleId,
        [FromForm] DateTime submittedAtUtc,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = await fileValidationService.ValidateBulkUploadFileAsync(file, FileUploadProfile.BulkUploadCsv, cancellationToken);
        return await validation.MatchAsync<ActionResult<MigrationImportResultDto>>(
            async validated =>
            {
                await using var content = validated.Content;
                var command = new ImportMigratedActivitiesCommand
                {
                    OrganisationName = organisationName,
                    DataCollectionId = dataCollectionId,
                    ModuleId = moduleId,
                    SubmittedAtUtc = submittedAtUtc,
                    FileStream = content,
                    FileName = validated.OriginalFileName,
                };

                var result = await mediator.Send(command, cancellationToken);
                return result.Match<ActionResult<MigrationImportResultDto>>(
                    success => Ok(success),
                    error => BadRequest(error));
            },
            error => Task.FromResult<ActionResult<MigrationImportResultDto>>(BadRequest(error.Message)));
    }
}