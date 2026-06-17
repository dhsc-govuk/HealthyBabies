using Domain.DataCollections.Forms.ValueObjects;

namespace Domain.DataCollections.Forms;

/// <summary>
/// Tracks the history of status changes for a form submission.
/// Provides a complete audit trail of all state transitions.
/// </summary>
public class FormSubmissionHistory : AuditableEntity<FormSubmissionHistoryId>
{
    /// <summary>
    /// Gets reference to the parent submission.
    /// </summary>
    public FormSubmissionId FormSubmissionId { get; private set; } = default!;
    public FormSubmission? FormSubmission { get; private set; }

    /// <summary>
    /// Gets status before the change.
    /// </summary>
    public SubmissionStatus PreviousStatus { get; private set; }

    /// <summary>
    /// Gets status after the change.
    /// </summary>
    public SubmissionStatus NewStatus { get; private set; }

    /// <summary>
    /// Gets notes or reason for the status change.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Gets timestamp of when this change occurred.
    /// </summary>
    public DateTime ChangedAt { get; private set; }

    private FormSubmissionHistory()
    {
    }

    private FormSubmissionHistory(
        FormSubmissionHistoryId id,
        FormSubmissionId formSubmissionId,
        SubmissionStatus previousStatus,
        SubmissionStatus newStatus,
        string? notes)
    {
        Id = id;
        FormSubmissionId = formSubmissionId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Notes = notes;
        ChangedAt = DateTime.UtcNow;
    }

    internal static FormSubmissionHistory Create(
        FormSubmissionId formSubmissionId,
        SubmissionStatus previousStatus,
        SubmissionStatus newStatus,
        string? notes = null)
    {
        return new FormSubmissionHistory(
            FormSubmissionHistoryId.New(),
            formSubmissionId,
            previousStatus,
            newStatus,
            notes);
    }
}