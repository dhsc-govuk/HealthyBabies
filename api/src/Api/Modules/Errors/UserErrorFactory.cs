using Application.Users.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class UserErrorFactory
{
    public static ObjectResult ToObjectResult(this UserException error)
    {
        var message = error switch
        {
            UserUnknownException unknownEx => $"{error.Message}: {GetFullExceptionMessage(unknownEx.InnerException)}",
            _ => error.Message
        };

        return new ObjectResult(new { error = error.GetType().Name, message })
        {
            StatusCode = error switch
            {
                UserExistsException => StatusCodes.Status409Conflict,
                UserDoesNotExistException => StatusCodes.Status404NotFound,
                UserUnknownException => StatusCodes.Status500InternalServerError,
                UserWrongRoleException or
                    UserOrganisationDoesNotExistException or
                    UserDoesNotBelongToOrganisationException or
                    UserLocationDoesNotExistException or
                UserArgumentException => StatusCodes.Status400BadRequest,
                _ => throw new NotImplementedException("User exception handler does not implemented")
            }
        };
    }

    private static string GetFullExceptionMessage(Exception? ex)
    {
        if (ex == null)
        {
            return "No details available";
        }

        var messages = new List<string>();
        var current = ex;
        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" -> ", messages);
    }
}