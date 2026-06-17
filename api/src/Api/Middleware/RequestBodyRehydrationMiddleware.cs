using System.Text;
using System.Text.Json;
using Application.Common.Constants;
using Application.Common.Interfaces;

namespace Api.Middleware;

public class RequestBodyRehydrationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyRehydrationMiddleware> _logger;

    public RequestBodyRehydrationMiddleware(
        RequestDelegate next,
        ILogger<RequestBodyRehydrationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRequestStagingService requestStagingService)
    {
        if (!context.Request.Headers.TryGetValue(
                RequestStagingConstants.BodyStagingIdHeader,
                out var headerValues))
        {
            await _next(context);
            return;
        }

        var headerValue = headerValues.ToString();
        if (!Guid.TryParse(headerValue, out var stagingId) || stagingId == Guid.Empty)
        {
            await WriteBadRequest(context, "Invalid X-Body-Staging-Id header");
            return;
        }

        await DrainStubBody(context, context.RequestAborted);

        var rehydrated = await requestStagingService.RehydrateAsync(stagingId, context.RequestAborted);

        await rehydrated.Match(
            body =>
            {
                ReplaceRequestBody(context, body);
                _logger.LogDebug(
                    "Rehydrated request body for stagingId {StagingId} ({ContentLength} bytes, {ContentType})",
                    stagingId,
                    body.ContentLength,
                    body.ContentType);
                return _next(context);
            },
            ex =>
            {
                _logger.LogWarning(
                    ex,
                    "Failed to rehydrate request body for stagingId {StagingId}",
                    stagingId);
                return WriteBadRequest(context, "Request body staging entry not found or expired");
            });
    }

    private static async Task DrainStubBody(HttpContext context, CancellationToken cancellationToken)
    {
        // Read and discard the small stub body sent by the client so MVC sees the rehydrated body.
        if (context.Request.ContentLength is 0 or null)
        {
            return;
        }

        try
        {
            using var memory = new MemoryStream();
            await context.Request.Body.CopyToAsync(memory, cancellationToken);
        }
        catch
        {
            // Stub body is unimportant — ignore any read failures.
        }
    }

    private static void ReplaceRequestBody(HttpContext context, RehydratedBody body)
    {
        context.Request.Body = body.Body;
        context.Request.ContentLength = body.ContentLength;
        context.Request.ContentType = body.ContentType;
        context.Request.Headers.ContentType = body.ContentType;
        context.Request.Headers.ContentLength = body.ContentLength;
    }

    private static async Task WriteBadRequest(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { error = "BODY_STAGING_INVALID", message });
        await context.Response.WriteAsync(payload, Encoding.UTF8);
    }
}

public static class RequestBodyRehydrationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestBodyRehydration(this IApplicationBuilder builder)
        => builder.UseMiddleware<RequestBodyRehydrationMiddleware>();
}