using Application.ServiceForms.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ServiceFormQuestionErrorFactory
{
    public static ObjectResult ToObjectResult(this ServiceFormQuestionException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                ServiceFormQuestionNotFoundException => StatusCodes.Status404NotFound,
                ServiceFormQuestionDuplicateCodeException => StatusCodes.Status409Conflict,
                ServiceFormQuestionCannotDeletePredefinedException => StatusCodes.Status400BadRequest,
                ServiceFormQuestionUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("ServiceFormQuestion exception handler not implemented")
            }
        };
    }
}