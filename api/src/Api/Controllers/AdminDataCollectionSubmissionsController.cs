using Api.Services.Abstract;
using Application.Submissions.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Support tooling for BLUM-7047: detects and repairs a data collection submission left
/// orphaned by a SubmitSubmission call that was cut off before the parent
/// DataCollectionSubmission record was written.
/// </summary>
[Route("admin/organisations/{organisationId:guid}/data-collections/{dataCollectionId:guid}")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class AdminDataCollectionSubmissionsController(
    ISubmissionsControllerService controllerService)
    : ControllerBase
{
    [HttpGet("submission-integrity")]
    public async Task<ActionResult<SubmissionIntegrityDto>> GetSubmissionIntegrity(
        [FromRoute] Guid organisationId,
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        return await controllerService.GetSubmissionIntegrity(organisationId, dataCollectionId, cancellationToken);
    }

    [HttpPost("reset-corrupted-submission")]
    public async Task<ActionResult<ResetCorruptedSubmissionResultDto>> ResetCorruptedSubmission(
        [FromRoute] Guid organisationId,
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        return await controllerService.ResetCorruptedSubmission(organisationId, dataCollectionId, cancellationToken);
    }
}