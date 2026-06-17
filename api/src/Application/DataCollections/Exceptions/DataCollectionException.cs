using Domain.DataCollections;

namespace Application.DataCollections.Exceptions;

public abstract class DataCollectionException(DataCollectionId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public DataCollectionId DataCollectionId { get; } = id;
}

public class DataCollectionAlreadyExistsException(DataCollectionId id, string name)
    : DataCollectionException(id, $"Data collection with name '{name}' already exists under id {id}");

public class DataCollectionDoesNotExistException(DataCollectionId id)
    : DataCollectionException(id, $"Data collection with id '{id}' does not exist");

public class DataCollectionUnknownException(DataCollectionId id, Exception innerException)
    : DataCollectionException(id, $"Unknown exception for the data collection {id}", innerException);