using Application.ServiceCategories.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ServiceCategoryErrorFactory
{
    public static ObjectResult ToObjectResult(this ServiceCategoryException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                ServiceCategoryAlreadyExistsException => StatusCodes.Status409Conflict,
                ServiceCategoryDoesNotExistException => StatusCodes.Status404NotFound,
                ServiceCategoryArgumentException => StatusCodes.Status400BadRequest,
                ServiceCategoryUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Service category exception handler not implemented")
            }
        };
    }
}