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

    /// <summary>
    /// Form modules the reviewer asked the local authority to focus on when reopening. Purely
    /// informational - the local authority can still amend any section regardless. Empty when
    /// no sections were selected, or for a decision that is not a reopen.
    /// </summary>
    public IReadOnlyList<Guid> SectionsToReview { get; private set; } = [];

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

    /// <summary>
    /// Records a DHSC decision that the plan is not accepted, with a mandatory reason.
    /// This is the action behind the "Reopen submission" button in the admin UI - the wording
    /// shown to users is "reopen", but the recorded decision is a rejection. The plan becomes
    /// editable again so the local authority can amend and resubmit; see IsLockedForEditing.
    /// Do not confuse this with <see cref="Reopen"/>, which serves the management information
    /// quarterly flow and records nothing.
    /// </summary>
    public void Reject(UserId reviewedBy, string notes, IReadOnlyList<Guid> sectionsToReview)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(notes, nameof(notes));
        Guard.Ensure(Status == DataCollectionSubmissionStatus.Submitted, "Can only reject submitted data collections");

        Status = DataCollectionSubmissionStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewedBy;
        ReviewNotes = notes;
        SectionsToReview = sectionsToReview;
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
            Status == DataCollectionSubmissionStatus.Rejected ||
            Status == DataCollectionSubmissionStatus.Submitted,
            "Can only reopen data collections that require changes, were rejected, or were submitted");

        Status = DataCollectionSubmissionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejected is deliberately absent: a rejection reopens the plan for amendment, so the
    /// local authority must be able to edit and resubmit. Only an in-flight submission awaiting
    /// review, or an approved (Complete) plan, is locked.
    /// </summary>
    public bool IsLockedForEditing() =>
        Status == DataCollectionSubmissionStatus.Submitted ||
        Status == DataCollectionSubmissionStatus.Approved;
}