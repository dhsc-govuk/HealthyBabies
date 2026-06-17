using Application.Organisations.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class LocationErrorFactory
{
    public static ObjectResult ToObjectResult(this LocationException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                LocationOrganisationDoesNotExistException => StatusCodes.Status404NotFound,
                LocationAlreadyExistsException or LocationReferenceNumberAlreadyExistsException => StatusCodes.Status409Conflict,
                LocationDoesNotExistException => StatusCodes.Status404NotFound,
                LocationArgumentException => StatusCodes.Status400BadRequest,
                LocationUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Location exception handler does not implemented")
            }
        };
    }
}