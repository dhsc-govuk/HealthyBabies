using Application.Organisations.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class OrganisationContactErrorFactory
{
    public static ObjectResult ToObjectResult(this OrganisationContactException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                OrganisationContactDoesNotExistException => StatusCodes.Status404NotFound,
                OrganisationContactInvalidRoleException => StatusCodes.Status400BadRequest,
                OrganisationContactUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Organisation contact exception handler not implemented")
            }
        };
    }
}