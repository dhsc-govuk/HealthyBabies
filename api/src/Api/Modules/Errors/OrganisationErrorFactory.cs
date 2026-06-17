using Application.Organisations.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class OrganisationErrorFactory
{
    public static ObjectResult ToObjectResult(this OrganisationException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                OrganisationAlreadyExistsException => StatusCodes.Status409Conflict,
                OrganisationDoesNotExistException => StatusCodes.Status404NotFound,
                OrganisationUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Organisation exception handler does not implemented")
            }
        };
    }
}