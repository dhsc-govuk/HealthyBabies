using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("system")]
[ApiController]
[Authorize]
public class RequestStagingController(IRequestStagingService requestStagingService) : ControllerBase
{
    public sealed record StageRequestBodyResponse(Guid StagingId, DateTime ExpiresAtUtc);

    [HttpPost("request-staging")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<ActionResult<StageRequestBodyResponse>> Stage(
        [FromForm] IFormFile body,
        CancellationToken cancellationToken)
    {
        if (body is null || body.Length == 0)
        {
            return BadRequest("Request body is required");
        }

        var declaredContentType = string.IsNullOrWhiteSpace(body.ContentType)
            ? "application/json"
            : body.ContentType;
        var userId = User.FindFirstValue("userId");

        await using var stream = body.OpenReadStream();
        var result = await requestStagingService.StageAsync(
            stream,
            declaredContentType,
            body.Length,
            userId,
            cancellationToken);

        return result.Match<ActionResult<StageRequestBodyResponse>>(
            descriptor => Ok(new StageRequestBodyResponse(descriptor.StagingId, descriptor.ExpiresAtUtc)),
            ex => StatusCode(StatusCodes.Status500InternalServerError, ex.Message));
    }
}