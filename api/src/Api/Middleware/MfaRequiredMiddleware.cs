using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Common.Settings;
using Domain.Users;

namespace Api.Middleware;

public class MfaRequiredMiddleware
{
    private const string MfaSessionCookieName = "mfa_session";
    private static readonly System.Collections.Generic.HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/mfa/status",
        "/mfa/setup",
        "/mfa/setup/verify",
        "/mfa/verify",
        "/mfa/verify/recovery",
        "/mfa/disable",
        "/mfa/recovery-codes/regenerate",
        "/mfa/logout"
    };
    private readonly RequestDelegate _next;

    public MfaRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdentityService identityService,
        IMfaSessionService sessionService,
        IUserMfaRepository userMfaRepository,
        ApplicationSettings settings)
    {
        // Allow bypassing MFA for integration tests
        if (settings.Mfa.BypassForTesting)
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        var userIdOption = identityService.GetUserId();
        if (userIdOption.IsNone)
        {
            await _next(context);
            return;
        }

        var userId = userIdOption.Match(id => id, () => throw new InvalidOperationException());
        var userMfaOption = await userMfaRepository.GetByUserIdAsync(userId);

        // If no MFA record exists, user must set up MFA
        if (userMfaOption.IsNone)
        {
            await WriteMfaSetupRequiredResponse(context);
            return;
        }

        var userMfa = userMfaOption.Match(mfa => mfa, () => throw new InvalidOperationException());

        // If MFA record exists but not enabled, allow access (MFA is disabled/relaxed)
        if (!userMfa.IsEnabled)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Cookies.TryGetValue(MfaSessionCookieName, out var sessionIdString) ||
            !Guid.TryParse(sessionIdString, out var sessionIdGuid))
        {
            await WriteMfaRequiredResponse(context);
            return;
        }

        var sessionId = new MfaSessionId(sessionIdGuid);
        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var session = await sessionService.ValidateSessionAsync(sessionId, ipAddress, userAgent);

        if (session == null || session.UserId != userId)
        {
            await WriteMfaRequiredResponse(context);
            return;
        }

        await _next(context);
    }

    private static bool IsExemptPath(string path)
    {
        foreach (var exemptPath in ExemptPaths)
        {
            if (path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WriteMfaRequiredResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "MFA_REQUIRED",
            message = "MFA verification required"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static async Task WriteMfaSetupRequiredResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "MFA_SETUP_REQUIRED",
            message = "MFA setup required"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Azure Front Door uses X-Azure-ClientIP for the original client IP
        var azureClientIp = context.Request.Headers["X-Azure-ClientIP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(azureClientIp))
        {
            return azureClientIp.Trim();
        }

        // Fallback to X-Forwarded-For (set by other load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public static class MfaRequiredMiddlewareExtensions
{
    public static IApplicationBuilder UseMfaRequired(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MfaRequiredMiddleware>();
    }
}