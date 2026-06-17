using Application.SiteForms.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class SiteFormQuestionErrorFactory
{
    public static ObjectResult ToObjectResult(this SiteFormQuestionException exception)
    {
        return exception switch
        {
            SiteFormQuestionNotFoundException => new NotFoundObjectResult(new { error = exception.Message }),
            SiteFormQuestionDuplicateCodeException => new ConflictObjectResult(new { error = exception.Message }),
            SiteFormQuestionCannotDeletePredefinedException => new BadRequestObjectResult(new { error = exception.Message }),
            SiteFormQuestionUnknownException => new ObjectResult(new { error = exception.Message }) { StatusCode = 500 },
            _ => new ObjectResult(new { error = exception.Message }) { StatusCode = 500 }
        };
    }
}