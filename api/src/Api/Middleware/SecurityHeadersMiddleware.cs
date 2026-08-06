using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Api.Middleware;

/// <summary>
/// Sets HTTP security response headers on every outbound response.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private const string HstsValue = "max-age=31536000; includeSubDomains";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME-type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Disallow framing — defence against clickjacking
        // (CSP frame-ancestors below is the modern equivalent; both are set for back-compat)
        headers["X-Frame-Options"] = "DENY";

        // Restrict browser features accessible to this origin and any embedded content
        headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), " +
            "magnetometer=(), microphone=(), payment=(), usb=()";

        // Prevent Referer header leaking sensitive URL parameters to third parties
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Block Flash and PDF plug-ins from loading cross-domain data
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Content Security Policy — this is a pure JSON API; it serves no scripts,
        // styles or HTML, so the strictest possible policy is correct here.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Prevent caching of API responses — all endpoints return authenticated data
        headers["Cache-Control"] = "no-store, max-age=0";
        headers["Pragma"] = "no-cache";

        // HSTS — only meaningful over HTTPS. Suppressed in development so local
        // HTTP traffic is not broken; set unconditionally in all other environments
        // so it fires correctly even when TLS is terminated at the load balancer.
        if (!env.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = HstsValue;
        }

        return next(context);
    }
}