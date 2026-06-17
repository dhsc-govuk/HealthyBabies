using Domain.Common;

namespace Domain.DataCollections.Forms;

/// <summary>
/// Represents an option for select, radio, or checkbox fields.
/// </summary>
public class FormFieldOption : AuditableEntity<FormFieldOptionId>
{
    /// <summary>
    /// Gets reference to the parent field.
    /// </summary>
    public FormFieldId FormFieldId { get; private set; } = default!;
    public FormField? FormField { get; private set; }

    /// <summary>
    /// Gets the value stored when this option is selected.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets display label shown to users.
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Gets order in which this option appears.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this option is selected by default.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this option is currently active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets child field that is shown/required when this option is selected.
    /// For example: "Yes" option triggers "What was the previous name?" text field.
    /// </summary>
    public FormFieldId? TriggeredFieldId { get; private set; }
    public FormField? TriggeredField { get; private set; }

    private FormFieldOption()
    {
    }

    private FormFieldOption(
        FormFieldOptionId id,
        FormFieldId formFieldId,
        string value,
        string label,
        int displayOrder,
        bool isDefault,
        FormFieldId? triggeredFieldId)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Id = id;
        FormFieldId = formFieldId;
        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
        TriggeredFieldId = triggeredFieldId;
    }

    internal static FormFieldOption Create(
        FormFieldId formFieldId,
        string value,
        string label,
        int displayOrder,
        bool isDefault = false,
        FormFieldId? triggeredFieldId = null)
    {
        return new FormFieldOption(
            FormFieldOptionId.New(),
            formFieldId,
            value,
            label,
            displayOrder,
            isDefault,
            triggeredFieldId);
    }

    public void UpdateDetails(string value, string label, int displayOrder, bool isDefault)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
    }

    /// <summary>
    /// Links a child field that should be shown when this option is selected.
    /// </summary>
    public void SetTriggeredField(FormFieldId? fieldId)
    {
        TriggeredFieldId = fieldId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}