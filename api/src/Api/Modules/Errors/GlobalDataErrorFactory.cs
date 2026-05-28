using Application.Systems.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class GlobalDataErrorFactory
{
    public static ObjectResult ToObjectResult(this GlobalDataException exception)
    {
        return exception switch
        {
            GlobalDataDoesNotExistException => new NotFoundObjectResult(exception.Message),
            GlobalDataInvalidEntityException => new BadRequestObjectResult(exception.Message),
            GlobalDataDuplicateValueException => new ConflictObjectResult(exception.Message),
            GlobalDataUnknownException => new ObjectResult(exception.Message)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            _ => new ObjectResult(exception.Message)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}