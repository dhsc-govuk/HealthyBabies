using Domain.Common;
using Domain.Users;

namespace Domain.DataCollections;

/// <summary>
/// An immutable, point-in-time copy of a Delivery Plan submission, taken the moment DHSC rejects
/// it - before the local authority's next edit can overwrite the answers being rejected. Written
/// once, in the same transaction as the DataCollectionSubmission.Reject() call that triggers it,
/// and never updated afterwards.
///
/// ReviewNotes, SectionsToReview and ResubmitBy are copied at creation rather than read live from
/// the parent DataCollectionSubmission later, because a second rejection overwrites those fields
/// on the parent - this version must keep its own record of what they were at the time.
/// </summary>
public class DataCollectionSubmissionVersion : AuditableEntity<DataCollectionSubmissionVersionId>
{
    public DataCollectionSubmissionId DataCollectionSubmissionId { get; private set; }
    public DataCollectionSubmission? DataCollectionSubmission { get; private set; }

    public int VersionNumber { get; private set; }

    public string? ReviewNotes { get; private set; }
    public IReadOnlyList<Guid> SectionsToReview { get; private set; } = [];
    public DateTime? ResubmitBy { get; private set; }

    public UserId ReviewedById { get; private set; }
    public User? ReviewedBy { get; private set; }
    public DateTime ReviewedAt { get; private set; }

    public UserId? SubmittedById { get; private set; }
    public User? SubmittedBy { get; private set; }
    public DateTime? SubmittedAt { get; private set; }

    /// <summary>
    /// The whole submission at this moment, section by section - the same JSON shape
    /// SubmissionExportService already produces for a download.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    private DataCollectionSubmissionVersion()
    {
        DataCollectionSubmissionId = DataCollectionSubmissionId.Empty();
    }

    private DataCollectionSubmissionVersion(
        DataCollectionSubmissionVersionId id,
        DataCollectionSubmissionId dataCollectionSubmissionId,
        int versionNumber,
        string? reviewNotes,
        IReadOnlyList<Guid> sectionsToReview,
        DateTime? resubmitBy,
        UserId reviewedById,
        DateTime reviewedAt,
        UserId? submittedById,
        DateTime? submittedAt,
        string content)
    {
        Guard.NotNull(dataCollectionSubmissionId, nameof(dataCollectionSubmissionId));
        Guard.Ensure(versionNumber > 0, "Version number must be positive");
        Guard.NotNullOrEmptyOrWhiteSpace(content, nameof(content));

        Id = id;
        DataCollectionSubmissionId = dataCollectionSubmissionId;
        VersionNumber = versionNumber;
        ReviewNotes = reviewNotes;
        SectionsToReview = sectionsToReview;
        ResubmitBy = resubmitBy;
        ReviewedById = reviewedById;
        ReviewedAt = reviewedAt;
        SubmittedById = submittedById;
        SubmittedAt = submittedAt;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollectionSubmissionVersion Create(
        DataCollectionSubmissionId dataCollectionSubmissionId,
        int versionNumber,
        string? reviewNotes,
        IReadOnlyList<Guid> sectionsToReview,
        DateTime? resubmitBy,
        UserId reviewedById,
        DateTime reviewedAt,
        UserId? submittedById,
        DateTime? submittedAt,
        string content)
    {
        var version = new DataCollectionSubmissionVersion(
            DataCollectionSubmissionVersionId.New(),
            dataCollectionSubmissionId,
            versionNumber,
            reviewNotes,
            sectionsToReview,
            resubmitBy,
            reviewedById,
            reviewedAt,
            submittedById,
            submittedAt,
            content);

        version.SetCreated(reviewedById);
        return version;
    }
}