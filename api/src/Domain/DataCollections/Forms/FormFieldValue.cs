namespace Domain.DataCollections.Forms;

/// <summary>
/// Represents the actual value entered for a field in a submission.
/// </summary>
public class FormFieldValue : AuditableEntity<FormFieldValueId>
{
    /// <summary>
    /// Gets reference to the parent submission.
    /// </summary>
    public FormSubmissionId FormSubmissionId { get; private set; } = default!;
    public FormSubmission? FormSubmission { get; private set; }

    /// <summary>
    /// Gets reference to the field definition.
    /// </summary>
    public FormFieldId FormFieldId { get; private set; } = default!;
    public FormField? FormField { get; private set; }

    /// <summary>
    /// Gets the stored value (serialized as string for flexibility).
    /// Complex values (arrays, objects) are stored as JSON.
    /// </summary>
    public string? Value { get; private set; }

    /// <summary>
    /// Gets human-readable display value (useful for select fields where value might be an ID).
    /// </summary>
    public string? DisplayValue { get; private set; }

    /// <summary>
    /// Gets the field key/code as it was when this answer was submitted (self-describing snapshot).
    /// Null for legacy rows submitted before snapshotting; read paths fall back to the live field.
    /// </summary>
    public string? QuestionCode { get; private set; }

    /// <summary>
    /// Gets the field label as it was when this answer was submitted.
    /// </summary>
    public string? QuestionLabel { get; private set; }

    /// <summary>
    /// Gets the field hint as it was when this answer was submitted.
    /// </summary>
    public string? QuestionHint { get; private set; }

    /// <summary>
    /// Gets the field type as it was when this answer was submitted (e.g. "radio", "checkbox").
    /// </summary>
    public string? FieldType { get; private set; }

    /// <summary>
    /// Gets the field display order as it was when this answer was submitted.
    /// </summary>
    public int? DisplayOrder { get; private set; }

    /// <summary>
    /// Gets a JSON snapshot of the field's selectable options (value/label pairs) at submission time,
    /// so a stored value still resolves to the label a user saw even if the option is later changed or removed.
    /// </summary>
    public string? OptionsSnapshot { get; private set; }

    /// <summary>
    /// Gets for file uploads, stores the file reference/path.
    /// </summary>
    public string? FileReference { get; private set; }

    /// <summary>
    /// Gets for file uploads, stores the original filename.
    /// </summary>
    public string? FileName { get; private set; }

    /// <summary>
    /// Gets for file uploads, stores the file size in bytes.
    /// </summary>
    public long? FileSize { get; private set; }

    /// <summary>
    /// Gets for file uploads, stores the MIME type.
    /// </summary>
    public string? FileMimeType { get; private set; }

    private FormFieldValue()
    {
    }

    private FormFieldValue(
        FormFieldValueId id,
        FormSubmissionId formSubmissionId,
        FormFieldId formFieldId,
        string? value,
        string? displayValue)
    {
        Id = id;
        FormSubmissionId = formSubmissionId;
        FormFieldId = formFieldId;
        Value = value;
        DisplayValue = displayValue;
    }

    internal static FormFieldValue Create(
        FormSubmissionId formSubmissionId,
        FormFieldId formFieldId,
        string? value,
        string? displayValue = null)
    {
        return new FormFieldValue(
            FormFieldValueId.New(),
            formSubmissionId,
            formFieldId,
            value,
            displayValue);
    }

    public void UpdateValue(string? value, string? displayValue = null)
    {
        Value = value;
        if (displayValue != null)
        {
            DisplayValue = displayValue;
        }
    }

    /// <summary>
    /// Captures the field's definition (code, label, hint, type, order and options) at submission time,
    /// so this answer is self-describing and no longer depends on the live form for rendering.
    /// </summary>
    public void SetSnapshot(
        string questionCode,
        string questionLabel,
        string? questionHint,
        string fieldType,
        int displayOrder,
        string? optionsSnapshot)
    {
        QuestionCode = questionCode;
        QuestionLabel = questionLabel;
        QuestionHint = questionHint;
        FieldType = fieldType;
        DisplayOrder = displayOrder;
        OptionsSnapshot = optionsSnapshot;
    }

    public void SetFileInfo(string fileReference, string fileName, long fileSize, string mimeType)
    {
        FileReference = fileReference;
        FileName = fileName;
        FileSize = fileSize;
        FileMimeType = mimeType;
    }

    public void ClearFileInfo()
    {
        FileReference = null;
        FileName = null;
        FileSize = null;
        FileMimeType = null;
    }
}