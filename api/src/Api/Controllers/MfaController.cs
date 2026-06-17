using Api.Modules.Errors;
using Application.Common.Interfaces;
using Application.Common.Permissions;
using Application.Mfa.Commands;
using Application.Mfa.Dtos;
using Application.Mfa.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("mfa")]
[ApiController]
[Authorize]
public class MfaController(
    ISender sender,
    IIdentityService identityService,
    PermissionsService permissionsService)
    : ControllerBase
{
    private const string MfaSessionCookieName = "mfa_session";

    [HttpGet("status")]
    public async Task<ActionResult<MfaStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaStatusDto>>(
            async uId =>
            {
                var query = new GetMfaStatusQuery(uId);
                var result = await sender.Send(query, cancellationToken);
                return Ok(result);
            },
            e => Task.FromResult<ActionResult<MfaStatusDto>>(Unauthorized(e.Message)));
    }

    [HttpPost("setup")]
    public async Task<ActionResult<MfaSetupResponseDto>> Setup(CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaSetupResponseDto>>(
            async uId =>
            {
                var email = identityService.GetEmail().Match(e => e, () => string.Empty);
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized("Email is missing from token");
                }

                var command = new SetupMfaCommand(uId, email);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<MfaSetupResponseDto>>(
                    dto => Ok(dto),
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult<MfaSetupResponseDto>>(Unauthorized(e.Message)));
    }

    [HttpPost("setup/verify")]
    public async Task<ActionResult<MfaSetupCompleteResponseDto>> VerifySetup(
        [FromBody] MfaVerifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaSetupCompleteResponseDto>>(
            async uId =>
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var command = new VerifyMfaSetupCommand(uId, request.Code, ipAddress, userAgent);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<MfaSetupCompleteResponseDto>>(
                    dto =>
                    {
                        SetMfaSessionCookie(dto.SessionId, dto.SessionExpiresAt);
                        return Ok(dto);
                    },
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult<MfaSetupCompleteResponseDto>>(Unauthorized(e.Message)));
    }

    [HttpPost("verify")]
    public async Task<ActionResult<MfaVerifyResponseDto>> Verify(
        [FromBody] MfaVerifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaVerifyResponseDto>>(
            async uId =>
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var command = new VerifyMfaCommand(uId, request.Code, ipAddress, userAgent);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<MfaVerifyResponseDto>>(
                    dto =>
                    {
                        SetMfaSessionCookie(dto.SessionId, dto.ExpiresAt);
                        return Ok(dto);
                    },
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult<MfaVerifyResponseDto>>(Unauthorized(e.Message)));
    }

    [HttpPost("verify/recovery")]
    public async Task<ActionResult<MfaVerifyResponseDto>> VerifyRecoveryCode(
        [FromBody] MfaVerifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaVerifyResponseDto>>(
            async uId =>
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var command = new VerifyRecoveryCodeCommand(uId, request.Code, ipAddress, userAgent);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<MfaVerifyResponseDto>>(
                    dto =>
                    {
                        SetMfaSessionCookie(dto.SessionId, dto.ExpiresAt);
                        return Ok(dto);
                    },
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult<MfaVerifyResponseDto>>(Unauthorized(e.Message)));
    }

    [HttpPost("disable")]
    public async Task<ActionResult> Disable(
        [FromBody] MfaVerifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult>(
            async uId =>
            {
                var command = new DisableMfaCommand(uId, request.Code);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult>(
                    _ =>
                    {
                        ClearMfaSessionCookie();
                        return NoContent();
                    },
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        ClearMfaSessionCookie();
        return NoContent();
    }

    [HttpPost("recovery-codes/regenerate")]
    public async Task<ActionResult<MfaRecoveryCodesDto>> RegenerateRecoveryCodes(
        [FromBody] MfaVerifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync<ActionResult<MfaRecoveryCodesDto>>(
            async uId =>
            {
                var command = new RegenerateRecoveryCodesCommand(uId, request.Code);
                var result = await sender.Send(command, cancellationToken);

                return result.Match<ActionResult<MfaRecoveryCodesDto>>(
                    dto => Ok(dto),
                    error => error.ToObjectResult());
            },
            e => Task.FromResult<ActionResult<MfaRecoveryCodesDto>>(Unauthorized(e.Message)));
    }

    private string GetClientIpAddress()
    {
        // Azure Front Door uses X-Azure-ClientIP for the original client IP
        var azureClientIp = Request.Headers["X-Azure-ClientIP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(azureClientIp))
        {
            return azureClientIp.Trim();
        }

        // Fallback to X-Forwarded-For (set by other load balancers/proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }

    private void SetMfaSessionCookie(Guid sessionId, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt
        };
        Response.Cookies.Append(MfaSessionCookieName, sessionId.ToString(), cookieOptions);
    }

    private void ClearMfaSessionCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        };
        Response.Cookies.Delete(MfaSessionCookieName, cookieOptions);
    }
}