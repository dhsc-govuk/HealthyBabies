using Domain.Services;

namespace Application.Services.Exceptions;

public abstract class ServiceException(ServiceId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ServiceId ServiceId { get; } = id;
}

public class ServiceAlreadyExistsException(ServiceId id, string name)
    : ServiceException(id, $"Service with name {name} already exists under id {id}");

public class ServiceDoesNotExistException(ServiceId id)
    : ServiceException(id, $"Service with id {id} does not exist");

public class ServiceArgumentException(ServiceId id, string message) : ServiceException(id, message);

public class ServiceUnknownException(ServiceId id, Exception innerException)
    : ServiceException(id, $"Unknown exception for the service {id}", innerException);