using Domain.Common;
using Domain.Organisations;
using Domain.Users;

namespace Domain.DataCollections;

public class DataCollectionSubmission : AuditableEntity<DataCollectionSubmissionId>
{
    public DataCollectionId DataCollectionId { get; private set; }
    public DataCollection? DataCollection { get; private set; }

    public OrganisationId OrganisationId { get; private set; }
    public Organisation? Organisation { get; private set; }

    public DataCollectionSubmissionStatus Status { get; private set; }

    public DateTime? SubmittedAt { get; private set; }
    public UserId? SubmittedById { get; private set; }
    public User? SubmittedBy { get; private set; }

    public DateTime? ReviewedAt { get; private set; }
    public UserId? ReviewedById { get; private set; }
    public User? ReviewedBy { get; private set; }
    public string? ReviewNotes { get; private set; }

    private DataCollectionSubmission()
    {
        DataCollectionId = DataCollectionId.Empty();
        OrganisationId = OrganisationId.Empty();
    }

    private DataCollectionSubmission(
        DataCollectionSubmissionId id,
        DataCollectionId dataCollectionId,
        OrganisationId organisationId)
    {
        Guard.NotNull(dataCollectionId, nameof(dataCollectionId));
        Guard.NotNull(organisationId, nameof(organisationId));

        Id = id;
        DataCollectionId = dataCollectionId;
        OrganisationId = organisationId;
        Status = DataCollectionSubmissionStatus.NotStarted;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollectionSubmission Create(
        DataCollectionId dataCollectionId,
        OrganisationId organisationId)
    {
        return new DataCollectionSubmission(
            DataCollectionSubmissionId.New(),
            dataCollectionId,
            organisationId);
    }

    public void SetStatus(DataCollectionSubmissionStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Submit(UserId submittedBy)
    {
        Status = DataCollectionSubmissionStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        SubmittedById = submittedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(UserId reviewedBy, string? notes = null)
    {
        Guard.Ensure(Status == DataCollectionSubmissionStatus.Submitted, "Can only approve submitted data collections");

        Status = DataCollectionSubmissionStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewedBy;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(UserId reviewedBy, string notes)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(notes, nameof(notes));
        Guard.Ensure(Status == DataCollectionSubmissionStatus.Submitted, "Can only reject submitted data collections");

        Status = DataCollectionSubmissionStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewedBy;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestChanges(UserId reviewedBy, string notes)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(notes, nameof(notes));
        Guard.Ensure(Status == DataCollectionSubmissionStatus.Submitted, "Can only request changes on submitted data collections");

        Status = DataCollectionSubmissionStatus.RequiresChanges;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewedBy;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Guard.Ensure(
            Status == DataCollectionSubmissionStatus.RequiresChanges ||
            Status == DataCollectionSubmissionStatus.Rejected,
            "Can only reopen data collections that require changes or were rejected");

        Status = DataCollectionSubmissionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }
}