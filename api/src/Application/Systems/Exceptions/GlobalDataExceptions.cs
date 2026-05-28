using Domain.Systems;

namespace Application.Systems.Exceptions;

public abstract class GlobalDataException(GlobalDataId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public GlobalDataId GlobalDataId { get; } = id;
}

public class GlobalDataDoesNotExistException(GlobalDataId id)
    : GlobalDataException(id, $"Global data with id {id} does not exist");

public class GlobalDataUnknownException(GlobalDataId id, Exception innerException)
    : GlobalDataException(id, $"Unknown exception for the global data {id}", innerException);

public class GlobalDataInvalidEntityException(string entity)
    : GlobalDataException(GlobalDataId.Empty(), $"The entity type '{entity}' is not valid. Valid entity types are: {string.Join(", ", GlobalDataEntityTypes.GetEntityNames())}");

public class GlobalDataDuplicateValueException(string entity, string value)
    : GlobalDataException(GlobalDataId.Empty(), $"A lookup value '{value}' already exists for entity '{entity}'");