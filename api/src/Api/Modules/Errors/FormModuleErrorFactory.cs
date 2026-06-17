using Application.DataCollections.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class FormModuleErrorFactory
{
    public static ObjectResult ToObjectResult(this FormModuleException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                FormModuleNotFoundException => StatusCodes.Status404NotFound,
                FormModuleDuplicateCodeException => StatusCodes.Status409Conflict,
                FormModuleHasFieldsException => StatusCodes.Status400BadRequest,
                FormSectionNotFoundException => StatusCodes.Status404NotFound,
                FormSectionHasFieldsException => StatusCodes.Status400BadRequest,
                _ => throw new NotImplementedException("FormModule exception handler not implemented")
            }
        };
    }
}