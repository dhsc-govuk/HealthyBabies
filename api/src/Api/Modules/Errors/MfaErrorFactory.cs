using Application.Mfa.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class MfaErrorFactory
{
    public static ObjectResult ToObjectResult(this MfaException exception)
    {
        return exception switch
        {
            MfaRequiredException => new ObjectResult(new { error = "MFA_REQUIRED", message = exception.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            MfaNotEnabledException => new ObjectResult(new { error = "MFA_NOT_ENABLED", message = exception.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            },
            MfaAlreadyEnabledException => new ConflictObjectResult(new { error = "MFA_ALREADY_ENABLED", message = exception.Message }),
            InvalidMfaCodeException => new ObjectResult(new { error = "INVALID_MFA_CODE", message = exception.Message })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            },
            InvalidRecoveryCodeException => new ObjectResult(new { error = "INVALID_RECOVERY_CODE", message = exception.Message })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            },
            MfaRateLimitException rateLimitEx => new ObjectResult(new
            {
                error = "MFA_RATE_LIMITED",
                message = exception.Message,
                lockedUntil = rateLimitEx.LockedUntil
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            },
            MfaSetupExpiredException => new ObjectResult(new { error = "MFA_SETUP_EXPIRED", message = exception.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            },
            MfaSetupPendingException => new ObjectResult(new { error = "MFA_SETUP_PENDING", message = exception.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            },
            MfaNotSetupException => new ObjectResult(new { error = "MFA_NOT_SETUP", message = exception.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            },
            MfaUnknownException => new ObjectResult(new { error = "MFA_ERROR", message = exception.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            _ => new ObjectResult(new { error = "MFA_ERROR", message = exception.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}