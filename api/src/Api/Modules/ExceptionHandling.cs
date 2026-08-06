using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Modules;

public static class ExceptionHandling
{
    public static IApplicationBuilder UseUnhandledExceptionHandler(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Api.UnhandledExceptionHandler");
                logger.LogError(ex, "Unhandled exception processing request {TraceId}", context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Something went wrong submitting this request.",
                    Detail = $"Refresh and try again. If this keeps happening, contact support with reference {context.TraceIdentifier}.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        });

        return app;
    }

    public static IApplicationBuilder UseValidationExceptionHandler(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        });

        return app;
    }
}