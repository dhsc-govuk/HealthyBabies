using Api.Services.Abstract;
using Application.Submissions.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Read-only view of a local authority's previous Delivery Plan submissions, for DHSC and internal
/// staff. Mirrors DeliveryPlanReviewController's route shape but serves the frozen snapshot taken
/// at the moment a submission was rejected, rather than the live submission.
/// </summary>
[Route("admin/data-collections/{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}/versions")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DeliveryPlanVersionController(ISubmissionsControllerService controllerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubmissionVersionSummaryDto>>> GetSubmissionVersions(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetSubmissionVersions(localAuthorityId, dataCollectionId, cancellationToken);
        return result.ToList();
    }

    [HttpGet("{versionNumber:int}")]
    public async Task<ActionResult<SubmissionVersionDetailDto>> GetSubmissionVersionDetail(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] int versionNumber,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetSubmissionVersionDetail(localAuthorityId, dataCollectionId, versionNumber, cancellationToken);
        return result.Match<ActionResult<SubmissionVersionDetailDto>>(r => r, NotFound());
    }

    [HttpGet("{versionNumber:int}/sections/{formModuleId:guid}")]
    public async Task<ActionResult<SubmissionVersionSectionDetailDto>> GetSubmissionVersionSection(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] int versionNumber,
        [FromRoute] Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetSubmissionVersionSection(localAuthorityId, dataCollectionId, versionNumber, formModuleId, cancellationToken);
        return result.Match<ActionResult<SubmissionVersionSectionDetailDto>>(r => r, NotFound());
    }

    [HttpGet("{versionNumber:int}/sections/{formModuleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<SubmissionVersionActivityDetailDto>> GetSubmissionVersionActivity(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] int versionNumber,
        [FromRoute] Guid formModuleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetSubmissionVersionActivity(localAuthorityId, dataCollectionId, versionNumber, formModuleId, activityId, cancellationToken);
        return result.Match<ActionResult<SubmissionVersionActivityDetailDto>>(r => r, NotFound());
    }

    [HttpGet("{versionNumber:int}/download")]
    public async Task<ActionResult> DownloadSubmissionVersion(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] int versionNumber,
        CancellationToken cancellationToken)
    {
        var zipResult = await controllerService.GetSubmissionVersionZip(localAuthorityId, dataCollectionId, versionNumber, cancellationToken);
        return zipResult.Match<ActionResult>(
            bytes => File(bytes, "application/zip", $"submission-version-{versionNumber}.zip"),
            NotFound());
    }
}