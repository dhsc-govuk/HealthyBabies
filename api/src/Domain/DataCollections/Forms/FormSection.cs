using Domain.Common;

namespace Domain.DataCollections.Forms;

/// <summary>
/// Represents a section/step within a form module.
/// Sections group related fields together and enable multi-step wizard forms.
/// e.g., "Step 1 of 3 - Add a service", "Step 2 of 3 - Service details".
/// </summary>
public class FormSection : AuditableEntity<FormSectionId>
{
    /// <summary>
    /// Gets reference to the parent form module.
    /// </summary>
    public DataCollectionFormModuleId FormModuleId { get; private set; } = default!;
    public DataCollectionFormModule? FormModule { get; private set; }

    /// <summary>
    /// Gets section number/order (1, 2, 3, etc.) - used for "Step X of Y" display.
    /// </summary>
    public int SectionNumber { get; private set; }

    /// <summary>
    /// Gets display title for the section (e.g., "Add a service").
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets optional description/subtitle shown below the title.
    /// e.g., "Tell us about the service offered so it can be added to your account.".
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets optional help text or link label (e.g., "Help with services").
    /// </summary>
    public string? HelpText { get; private set; }

    /// <summary>
    /// Gets optional URL for help link.
    /// </summary>
    public string? HelpUrl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this section is currently active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    private FormSection()
    {
    }

    private FormSection(
        FormSectionId id,
        DataCollectionFormModuleId formModuleId,
        int sectionNumber,
        string title,
        string? description,
        string? helpText,
        string? helpUrl)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(title, nameof(title));

        Id = id;
        FormModuleId = formModuleId;
        SectionNumber = sectionNumber;
        Title = title;
        Description = description;
        HelpText = helpText;
        HelpUrl = helpUrl;
    }

    internal static FormSection Create(
        DataCollectionFormModuleId formModuleId,
        int sectionNumber,
        string title,
        string? description = null,
        string? helpText = null,
        string? helpUrl = null)
    {
        return new FormSection(
            FormSectionId.New(),
            formModuleId,
            sectionNumber,
            title,
            description,
            helpText,
            helpUrl);
    }

    public void UpdateDetails(
        string title,
        string? description,
        string? helpText,
        string? helpUrl)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(title, nameof(title));

        Title = title;
        Description = description;
        HelpText = helpText;
        HelpUrl = helpUrl;
    }

    public void UpdateSectionNumber(int sectionNumber)
    {
        SectionNumber = sectionNumber;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}