using Application.DataCollections.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class DataCollectionErrorFactory
{
    public static ObjectResult ToObjectResult(this DataCollectionException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                DataCollectionAlreadyExistsException => StatusCodes.Status409Conflict,
                DataCollectionDoesNotExistException => StatusCodes.Status404NotFound,
                DataCollectionUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("DataCollection exception handler not implemented")
            }
        };
    }
}