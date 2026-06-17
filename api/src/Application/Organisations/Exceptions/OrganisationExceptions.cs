using Domain.Organisations;

namespace Application.Organisations.Exceptions;

public abstract class OrganisationException(OrganisationId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public OrganisationId OrganisationId { get; } = id;
}

public class OrganisationAlreadyExistsException(OrganisationId id, string name)
    : OrganisationException(id, $"Organisation with name {name} already exists under id {id}");

public class OrganisationDoesNotExistException(OrganisationId id)
    : OrganisationException(id, $"Organisation with id {id} does not exist");

public class OrganisationUnknownException(OrganisationId id, Exception innerException)
    : OrganisationException(id, $"Unknown exception for the organisation {id}", innerException);