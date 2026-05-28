using Domain.Common;
using Domain.DataCollections.Forms.ValueObjects;

namespace Domain.DataCollections.Forms;

/// <summary>
/// Represents a field definition within a form module.
/// Defines the structure, type, and validation rules for a form field.
/// </summary>
public class FormField : AuditableEntity<FormFieldId>
{
    private readonly List<FormFieldOption> _options = new();

    /// <summary>
    /// Gets reference to the parent form module.
    /// </summary>
    public DataCollectionFormModuleId FormModuleId { get; private set; } = default!;
    public DataCollectionFormModule? FormModule { get; private set; }

    /// <summary>
    /// Gets reference to the section this field belongs to.
    /// Enables grouping fields into steps/pages (e.g., "Step 1 of 3").
    /// </summary>
    public FormSectionId? FormSectionId { get; private set; }
    public FormSection? FormSection { get; private set; }

    /// <summary>
    /// Gets unique key/identifier for this field within the form (e.g., "first_name", "email").
    /// Used for data binding and API responses.
    /// </summary>
    public string FieldKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets display label shown to users.
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Gets type of the field (text, number, select, etc.).
    /// </summary>
    public FieldType FieldType { get; private set; }

    /// <summary>
    /// Gets order in which this field appears in the form.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this field is required.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Gets placeholder text for input fields.
    /// </summary>
    public string? Placeholder { get; private set; }

    /// <summary>
    /// Gets help text/description shown below the field.
    /// </summary>
    public string? HelpText { get; private set; }

    /// <summary>
    /// Gets default value for the field.
    /// </summary>
    public string? DefaultValue { get; private set; }

    /// <summary>
    /// Gets jSON string containing validation rules (min, max, pattern, etc.).
    /// </summary>
    public string? ValidationRules { get; private set; }

    /// <summary>
    /// Gets jSON string containing conditional display rules.
    /// e.g.
    /// {
    ///   "showWhen": {
    ///     "fieldKey": "has_name_changed",
    ///     "equals": "yes"
    ///   }
    /// }.
    /// </summary>
    public string? ConditionalRules { get; private set; }

    /// <summary>
    /// Gets jSON string containing additional configuration (e.g., file types, max size).
    /// </summary>
    public string? Configuration { get; private set; }

    /// <summary>
    /// Gets parent field ID for nested/repeater fields.
    /// </summary>
    public FormFieldId? ParentFieldId { get; private set; }
    public FormField? ParentField { get; private set; }

    /// <summary>
    /// Gets reference to a reusable data source for options (e.g., "service-categories").
    /// When set, options are loaded from the DataSource instead of FormFieldOptions.
    /// </summary>
    public DataSourceId? DataSourceId { get; private set; }
    public DataSource? DataSource { get; private set; }

    /// <summary>
    /// Gets for hierarchical data sources, filter items by this parent value.
    /// e.g., Show only categories under "parent-infant-relationships" strand.
    /// </summary>
    public string? DataSourceParentValue { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this field is currently active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets inline options for select, radio, checkbox fields.
    /// Use this for simple, form-specific options.
    /// For reusable options, use DataSourceId instead.
    /// </summary>
    public IReadOnlyCollection<FormFieldOption> Options => _options.AsReadOnly();

    private FormField()
    {
    }

    private FormField(
        FormFieldId id,
        DataCollectionFormModuleId formModuleId,
        string fieldKey,
        string label,
        FieldType fieldType,
        int displayOrder,
        bool isRequired,
        string? placeholder,
        string? helpText,
        string? defaultValue,
        string? validationRules)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(fieldKey, nameof(fieldKey));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Id = id;
        FormModuleId = formModuleId;
        FieldKey = fieldKey;
        Label = label;
        FieldType = fieldType;
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
        Placeholder = placeholder;
        HelpText = helpText;
        DefaultValue = defaultValue;
        ValidationRules = validationRules;
    }

    internal static FormField Create(
        DataCollectionFormModuleId formModuleId,
        string fieldKey,
        string label,
        FieldType fieldType,
        int displayOrder,
        bool isRequired = false,
        string? placeholder = null,
        string? helpText = null,
        string? defaultValue = null,
        string? validationRules = null)
    {
        return new FormField(
            FormFieldId.New(),
            formModuleId,
            fieldKey,
            label,
            fieldType,
            displayOrder,
            isRequired,
            placeholder,
            helpText,
            defaultValue,
            validationRules);
    }

    public void UpdateDetails(
        string label,
        int displayOrder,
        bool isRequired,
        string? placeholder,
        string? helpText,
        string? defaultValue)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Label = label;
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
        Placeholder = placeholder;
        HelpText = helpText;
        DefaultValue = defaultValue;
    }

    public void SetValidationRules(string? rules) => ValidationRules = rules;

    public void SetConditionalRules(string? rules) => ConditionalRules = rules;

    public void SetConfiguration(string? config) => Configuration = config;

    public void SetParentField(FormFieldId? parentId) => ParentFieldId = parentId;

    /// <summary>
    /// Assigns this field to a section/step.
    /// </summary>
    public void SetSection(FormSectionId? sectionId) => FormSectionId = sectionId;

    /// <summary>
    /// Sets the field type.
    /// </summary>
    public void SetFieldType(FieldType fieldType) => FieldType = fieldType;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Sets a reusable data source for this field's options.
    /// </summary>
    /// <param name="dataSourceId">The data source to use.</param>
    /// <param name="parentValue">Optional parent value for hierarchical filtering.</param>
    public void SetDataSource(DataSourceId? dataSourceId, string? parentValue = null)
    {
        DataSourceId = dataSourceId;
        DataSourceParentValue = parentValue;
    }

    /// <summary>
    /// Adds an option for select/radio/checkbox fields.
    /// </summary>
    /// <returns></returns>
    public FormFieldOption AddOption(string value, string label, int displayOrder, bool isDefault = false)
    {
        var option = FormFieldOption.Create(Id, value, label, displayOrder, isDefault);
        _options.Add(option);
        return option;
    }

    /// <summary>
    /// Removes an option.
    /// </summary>
    public void RemoveOption(FormFieldOptionId optionId)
    {
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            _options.Remove(option);
        }
    }
}