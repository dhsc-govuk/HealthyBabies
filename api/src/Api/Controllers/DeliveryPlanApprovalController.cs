using System.Text;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common.Permissions;
using Application.DataCollections.Commands;
using Application.DataCollections.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Write path for reviewing a local authority's submitted Delivery Plan. Kept separate from
/// DeliveryPlanReviewController, which is read-only by design.
///
/// Only the admin role can reach these endpoints. LA and DfE users hold organisation admin or
/// delivery plan lead, so they are excluded by the role check alone.
///
/// Note the deliberate naming mismatch on reopen: the endpoint and the UI say "reopen", the
/// domain records a rejection. See ReopenDeliveryPlanSubmissionCommand.
/// </summary>
[Route("admin/data-collections/{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DeliveryPlanApprovalController(
    ISender sender,
    PermissionsService permissionsService,
    ISubmissionExportService submissionExportService) : ControllerBase
{
    [HttpPost("approve")]
    public async Task<ActionResult<SubmissionReviewResultDto>> Approve(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromBody] ApproveSubmissionDto? dto,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();

        return await userId.MatchAsync<ActionResult<SubmissionReviewResultDto>>(
            async currentUserId =>
            {
                var command = new ApproveDeliveryPlanSubmissionCommand
                {
                    DataCollectionId = dataCollectionId,
                    LocalAuthorityId = localAuthorityId,
                    ReviewedByUserId = currentUserId.Value,
                    Comment = dto?.Comment
                };

                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<SubmissionReviewResultDto>>(
                    submission => SubmissionReviewResultDto.FromDomainModel(submission),
                    error => error.ToObjectResult());
            },
            exception => Unauthorized(exception.Message));
    }

    [HttpPost("reopen")]
    public async Task<ActionResult<SubmissionReviewResultDto>> Reopen(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromBody] ReopenSubmissionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();

        return await userId.MatchAsync<ActionResult<SubmissionReviewResultDto>>(
            async currentUserId =>
            {
                // Exported here, not inside the command handler - ISubmissionExportService lives in
                // this project (Api), and the command must not depend on it. The reject action's
                // atomicity comes from the write side (submission, due date, and this content are
                // all staged and committed in one SaveChangesAsync inside the handler); reading the
                // current state slightly earlier, in the same request, before anything else can
                // change it, doesn't weaken that.
                var exportResult = await submissionExportService.ExportSubmissionAsJsonAsync(
                    dataCollectionId, localAuthorityId, cancellationToken);

                return await exportResult.MatchAsync<ActionResult<SubmissionReviewResultDto>>(
                    async content =>
                    {
                        var command = new ReopenDeliveryPlanSubmissionCommand
                        {
                            DataCollectionId = dataCollectionId,
                            LocalAuthorityId = localAuthorityId,
                            ReviewedByUserId = currentUserId.Value,
                            Reason = dto.Reason,
                            ResubmitBy = dto.ResubmitBy,
                            SectionsToReview = dto.SectionsToReview ?? [],
                            Content = Encoding.UTF8.GetString(content)
                        };

                        var result = await sender.Send(command, cancellationToken);

                        return result.Match<ActionResult<SubmissionReviewResultDto>>(
                            submission => SubmissionReviewResultDto.FromDomainModel(submission),
                            error => error.ToObjectResult());
                    },
                    error => BadRequest(error));
            },
            exception => Unauthorized(exception.Message));
    }
}