using Application.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ServiceErrorFactory
{
    public static ObjectResult ToObjectResult(this ServiceException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                ServiceAlreadyExistsException => StatusCodes.Status409Conflict,
                ServiceDoesNotExistException => StatusCodes.Status404NotFound,
                ServiceArgumentException => StatusCodes.Status400BadRequest,
                ServiceUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Service exception handler does not implemented")
            }
        };
    }
}