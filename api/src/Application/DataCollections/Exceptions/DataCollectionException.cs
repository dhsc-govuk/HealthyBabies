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

public class DataCollectionInvalidDateRangeException(DataCollectionId id, string message)
    : DataCollectionException(id, message);

public class DataCollectionUnknownException(DataCollectionId id, Exception innerException)
    : DataCollectionException(id, $"Unknown exception for the data collection {id}", innerException);

public abstract class DataCollectionSubmissionException(string message) : Exception(message);

public class DataCollectionSubmissionNotFoundException(Guid dataCollectionId, Guid localAuthorityId)
    : DataCollectionSubmissionException($"No submission exists for data collection '{dataCollectionId}' and local authority '{localAuthorityId}'");

public class DataCollectionSubmissionNotAwaitingReviewException(string currentStatus)
    : DataCollectionSubmissionException($"Only a submitted delivery plan can be reviewed. This plan is currently '{currentStatus}'.");

public class DataCollectionSubmissionWrongTypeException(string currentType)
    : DataCollectionSubmissionException($"Only a Delivery Plan submission can be accepted or reopened here. This data collection is '{currentType}'.");

public class DataCollectionSubmissionUnknownException(Exception innerException)
    : DataCollectionSubmissionException($"Unknown exception while reviewing the submission: {innerException.Message}");