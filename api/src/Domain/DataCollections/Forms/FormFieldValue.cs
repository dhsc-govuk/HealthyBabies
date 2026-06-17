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