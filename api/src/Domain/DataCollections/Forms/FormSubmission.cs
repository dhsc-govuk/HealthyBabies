using Domain.Common;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;

namespace Domain.DataCollections.Forms;

/// <summary>
/// Represents a submission of form data against a specific form module.
/// Supports draft mode for saving incomplete submissions.
/// </summary>
public class FormSubmission : SoftDeletableEntity<FormSubmissionId>
{
    private readonly List<FormFieldValue> _fieldValues = new();
    private readonly List<FormSubmissionHistory> _history = new();

    /// <summary>
    /// Gets reference to the form module this submission is for.
    /// </summary>
    public DataCollectionFormModuleId FormModuleId { get; private set; } = default!;
    public DataCollectionFormModule? FormModule { get; private set; }

    /// <summary>
    /// Gets current status of the submission.
    /// </summary>
    public SubmissionStatus Status { get; private set; }

    /// <summary>
    /// Reference to the organisation this submission belongs to.
    /// </summary>
    public OrganisationId? OrganisationId { get; private set; }

    /// <summary>
    /// Reference to the data collection this submission is for.
    /// </summary>
    public DataCollectionId? DataCollectionId { get; private set; }

    /// <summary>
    /// Reference to the entity this submission is for (e.g., ServiceId, ReferralId).
    /// Allows linking form data to other domain entities.
    /// </summary>
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// Gets when the submission was first saved as draft.
    /// </summary>
    public DateTime? DraftSavedAt { get; private set; }

    /// <summary>
    /// Gets when the submission was submitted (moved from draft to submitted).
    /// </summary>
    public DateTime? SubmittedAt { get; private set; }

    /// <summary>
    /// Gets who submitted the form.
    /// </summary>
    public UserId? SubmittedById { get; private set; }
    public User? SubmittedBy { get; private set; }

    /// <summary>
    /// Gets when the submission was last reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// Gets who reviewed the submission.
    /// </summary>
    public UserId? ReviewedById { get; private set; }
    public User? ReviewedBy { get; private set; }

    /// <summary>
    /// Gets notes from the reviewer.
    /// </summary>
    public string? ReviewNotes { get; private set; }

    /// <summary>
    /// Gets iP address of the submitter (for audit purposes).
    /// </summary>
    public string? SubmitterIpAddress { get; private set; }

    /// <summary>
    /// Gets user agent of the submitter (for audit purposes).
    /// </summary>
    public string? SubmitterUserAgent { get; private set; }

    /// <summary>
    /// Gets field values in this submission.
    /// </summary>
    public IReadOnlyCollection<FormFieldValue> FieldValues => _fieldValues.AsReadOnly();

    /// <summary>
    /// Gets history of status changes.
    /// </summary>
    public IReadOnlyCollection<FormSubmissionHistory> History => _history.AsReadOnly();

    private FormSubmission()
    {
    }

    private FormSubmission(
        FormSubmissionId id,
        DataCollectionFormModuleId formModuleId,
        OrganisationId? organisationId,
        DataCollectionId? dataCollectionId,
        string? entityType,
        Guid? entityId)
    {
        Id = id;
        FormModuleId = formModuleId;
        OrganisationId = organisationId;
        DataCollectionId = dataCollectionId;
        EntityType = entityType;
        EntityId = entityId;
        Status = SubmissionStatus.Draft;
        DraftSavedAt = DateTime.UtcNow;
    }

    public static FormSubmission Create(
        DataCollectionFormModuleId formModuleId,
        OrganisationId? organisationId = null,
        DataCollectionId? dataCollectionId = null,
        string? entityType = null,
        Guid? entityId = null)
    {
        return new FormSubmission(
            FormSubmissionId.New(),
            formModuleId,
            organisationId,
            dataCollectionId,
            entityType,
            entityId);
    }

    /// <summary>
    /// Sets or updates a field value.
    /// </summary>
    /// <returns></returns>
    public FormFieldValue SetFieldValue(
        FormFieldId fieldId,
        string? value,
        string? displayValue = null)
    {
        var existing = _fieldValues.FirstOrDefault(v => v.FormFieldId == fieldId);
        if (existing != null)
        {
            existing.UpdateValue(value, displayValue);
            return existing;
        }

        var fieldValue = FormFieldValue.Create(Id, fieldId, value, displayValue);
        _fieldValues.Add(fieldValue);
        return fieldValue;
    }

    /// <summary>
    /// Removes a field value.
    /// </summary>
    public void RemoveFieldValue(FormFieldId fieldId)
    {
        var existing = _fieldValues.FirstOrDefault(v => v.FormFieldId == fieldId);
        if (existing != null)
        {
            _fieldValues.Remove(existing);
        }
    }

    /// <summary>
    /// Saves the current state as a draft.
    /// </summary>
    public void SaveAsDraft()
    {
        // if (Status != SubmissionStatus.Draft && Status != SubmissionStatus.RequiresChanges)
        // {
        //    throw new InvalidOperationException("Can only save as draft when in Draft or RequiresChanges status.");
        // }
        DraftSavedAt = DateTime.UtcNow;
        AddHistoryEntry(Status, "Draft saved");
    }

    /// <summary>
    /// Submits the form for review/processing.
    /// </summary>
    public void Submit(UserId submittedBy, string? ipAddress = null, string? userAgent = null)
    {
        if (Status != SubmissionStatus.Draft && Status != SubmissionStatus.RequiresChanges)
        {
            throw new InvalidOperationException("Can only submit when in Draft or RequiresChanges status.");
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        SubmittedById = submittedBy;
        SubmitterIpAddress = ipAddress;
        SubmitterUserAgent = userAgent;

        AddHistoryEntry(previousStatus, "Submitted for review");
    }

    /// <summary>
    /// Marks the submission as under review.
    /// </summary>
    public void StartReview(UserId reviewerId)
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Can only start review on submitted forms.");
        }

        var previousStatus = Status;
        Status = SubmissionStatus.UnderReview;
        ReviewedById = reviewerId;

        AddHistoryEntry(previousStatus, "Review started");
    }

    /// <summary>
    /// Approves the submission.
    /// </summary>
    public void Approve(UserId reviewerId, string? notes = null)
    {
        if (Status != SubmissionStatus.UnderReview && Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Can only approve submitted or under-review forms.");
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewerId;
        ReviewNotes = notes;

        AddHistoryEntry(previousStatus, notes ?? "Approved");
    }

    /// <summary>
    /// Rejects the submission.
    /// </summary>
    public void Reject(UserId reviewerId, string? notes = null)
    {
        if (Status != SubmissionStatus.UnderReview && Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Can only reject submitted or under-review forms.");
        }

        var previousStatus = Status;
        Status = SubmissionStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewerId;
        ReviewNotes = notes;

        AddHistoryEntry(previousStatus, notes ?? "Rejected");
    }

    /// <summary>
    /// Requests changes to the submission.
    /// </summary>
    public void RequestChanges(UserId reviewerId, string notes)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(notes, nameof(notes));

        if (Status != SubmissionStatus.UnderReview && Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Can only request changes on submitted or under-review forms.");
        }

        var previousStatus = Status;
        Status = SubmissionStatus.RequiresChanges;
        ReviewedAt = DateTime.UtcNow;
        ReviewedById = reviewerId;
        ReviewNotes = notes;

        AddHistoryEntry(previousStatus, notes);
    }

    /// <summary>
    /// Cancels the submission.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        var previousStatus = Status;
        Status = SubmissionStatus.Cancelled;

        AddHistoryEntry(previousStatus, reason ?? "Cancelled");
    }

    /// <summary>
    /// Marks the submission as complete (for form modules that don't require review).
    /// </summary>
    public void MarkAsComplete()
    {
        var previousStatus = Status;
        Status = SubmissionStatus.Approved;
        ReviewedAt = DateTime.UtcNow;

        AddHistoryEntry(previousStatus, "Marked as complete");
    }

    /// <summary>
    /// Marks the submission as submitted (final submission of all completed forms).
    /// </summary>
    public void MarkAsSubmitted()
    {
        var previousStatus = Status;
        Status = SubmissionStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        AddHistoryEntry(previousStatus, "Final submission");
    }

    /// <summary>
    /// Links this submission to an entity.
    /// </summary>
    public void LinkToEntity(string entityType, Guid entityId)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(entityType, nameof(entityType));

        EntityType = entityType;
        EntityId = entityId;
    }

    private void AddHistoryEntry(SubmissionStatus previousStatus, string notes)
    {
        var entry = FormSubmissionHistory.Create(Id, previousStatus, Status, notes);
        _history.Add(entry);
    }
}