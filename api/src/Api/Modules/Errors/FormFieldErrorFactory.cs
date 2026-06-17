using Application.DataCollections.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class FormFieldErrorFactory
{
    public static ObjectResult ToObjectResult(this FormFieldException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                FormFieldNotFoundException => StatusCodes.Status404NotFound,
                FormFieldDuplicateKeyException => StatusCodes.Status409Conflict,
                FormFieldFormModuleNotFoundException => StatusCodes.Status404NotFound,
                FormFieldHasValuesException => StatusCodes.Status409Conflict,
                FormFieldUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("FormField exception handler not implemented")
            }
        };
    }
}