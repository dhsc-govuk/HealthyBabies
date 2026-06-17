using Domain.Organisations;

namespace Application.Organisations.Exceptions;

public abstract class OrganisationContactException(OrganisationContactId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public OrganisationContactId OrganisationContactId { get; } = id;
}

public class OrganisationContactDoesNotExistException(OrganisationContactId id)
    : OrganisationContactException(id, $"Organisation contact with id {id} does not exist");

public class OrganisationContactUnknownException(OrganisationContactId id, Exception innerException)
    : OrganisationContactException(id, $"Unknown exception for the organisation contact {id}", innerException);

public class OrganisationContactInvalidRoleException(OrganisationContactId id, string role)
    : OrganisationContactException(id, $"The role '{role}' is not a valid contact role. Please select a valid role from the available options.");