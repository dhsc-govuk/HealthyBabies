using Api.Services.Abstract;
using Application.DataCollections.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Read-only review of a local authority's submitted Delivery Plan, for DHSC and internal staff —
/// all of whom hold the existing admin role. Every action is a GET; approving, rejecting, commenting
/// on and editing a submission are all out of scope, so this controller exposes no write path.
///
/// Endpoints return 404 rather than 403 when the collection is not a Delivery Plan, so the review
/// surface simply does not exist for management information collections.
/// </summary>
[Route("admin/data-collections/{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}/review")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DeliveryPlanReviewController(IDeliveryPlanReviewService reviewService) : ControllerBase
{
    [HttpGet("sections/{formModuleId:guid}")]
    public async Task<ActionResult<ReviewSectionDetailDto>> GetSection(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.GetSection(dataCollectionId, localAuthorityId, formModuleId, cancellationToken);

        return result.Match<ActionResult<ReviewSectionDetailDto>>(
            r => r,
            NotFound());
    }

    [HttpGet("sections/{formModuleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<ReviewActivityDetailDto>> GetActivity(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromRoute] Guid formModuleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.GetActivity(dataCollectionId, localAuthorityId, formModuleId, activityId, cancellationToken);

        return result.Match<ActionResult<ReviewActivityDetailDto>>(
            r => r,
            NotFound());
    }
}