using Domain.Locations;
using Domain.Organisations;

namespace Application.Organisations.Exceptions;

public abstract class LocationException(LocationId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public LocationId LocationId { get; } = id;
}

public class LocationOrganisationDoesNotExistException(LocationId id, OrganisationId organisationId)
    : LocationException(id, $"Organisation with id {organisationId} does not exist")
{
    public OrganisationId OrganisationId { get; } = organisationId;
}

public class LocationAlreadyExistsException(LocationId id, string name)
    : LocationException(id, $"Location with name {name} already exists under id {id}");

public class LocationReferenceNumberAlreadyExistsException(LocationId id, string referenceNumber)
    : LocationException(id, $"Location with reference number {referenceNumber} already exists under id {id}");

public class LocationDoesNotExistException(LocationId id)
    : LocationException(id, $"Location with id {id} does not exist");

public class LocationArgumentException(LocationId id, string message) : LocationException(id, message);

public class LocationUnknownException(LocationId id, Exception innerException)
    : LocationException(id, $"Unknown exception for the location {id}", innerException);