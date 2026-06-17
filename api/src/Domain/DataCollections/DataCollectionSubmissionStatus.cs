namespace Domain.DataCollections;

public enum DataCollectionSubmissionStatus
{
    NotStarted = 0,
    InProgress = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    RequiresChanges = 5
}