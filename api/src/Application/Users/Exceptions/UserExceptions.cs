using Domain.Locations;
using Domain.Organisations;
using Domain.Users;

namespace Application.Users.Exceptions;

public abstract class UserException(UserId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public UserId UserId { get; } = id;
}

public class UserExistsException(UserId id, string email)
    : UserException(id, $"User with email {email} already exists under id {id}");

public class UserDoesNotExistException(UserId id) : UserException(id, $"User with id {id} does not exist");

public class UserWrongRoleException(UserId id, string requiredRole)
    : UserException(id, $"User with id {id} does not belong to role {requiredRole}")
{
    public string RequiredRole { get; } = requiredRole;
}

public class UserUnknownException(UserId id, Exception innerException)
    : UserException(id, $"Unknown error for user with id {id}", innerException);

public class UserOrganisationDoesNotExistException(UserId id, OrganisationId organisationId)
    : UserException(id, $"Organisation with id {organisationId} does not exist");

public class UserDoesNotBelongToOrganisationException(UserId id, OrganisationId organisationId) : UserException(id,
    $"User with id {id} does not belong to organisation with id {organisationId}");

public class UserLocationDoesNotExistException(UserId id, LocationId locationId)
    : UserException(id, $"Location with id {locationId} does not exist");

public class UserArgumentException(UserId id, string message) : UserException(id, message);