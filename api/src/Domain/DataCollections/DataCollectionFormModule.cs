using Domain.Common;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Users;

namespace Domain.DataCollections;

public class DataCollectionFormModule : AuditableEntity<DataCollectionFormModuleId>
{
    private readonly List<FormSection> _sections = new();
    private readonly List<FormField> _fields = new();

    public string Code { get; private set; }
    public int SectionNumber { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime LastChangedOn { get; private set; }
    public bool IsActive { get; private set; }

    public FormStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public UserId? PublishedById { get; private set; }
    public User? PublishedBy { get; private set; }
    public string? ValidationSchema { get; private set; }

    public IReadOnlyCollection<FormSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<FormField> Fields => _fields.AsReadOnly();

    private DataCollectionFormModule()
    {
        Code = string.Empty;
        Name = string.Empty;
        Status = FormStatus.Draft;
    }

    private DataCollectionFormModule(
        DataCollectionFormModuleId id,
        string code,
        int sectionNumber,
        string name,
        string? description,
        DateTime lastChangedOn,
        bool isActive)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(code, nameof(code));
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.GreaterThan(sectionNumber, 0, nameof(sectionNumber));

        Id = id;
        Code = code;
        SectionNumber = sectionNumber;
        Name = name;
        Description = description;
        LastChangedOn = lastChangedOn;
        IsActive = isActive;
        Status = FormStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollectionFormModule Create(
        DataCollectionFormModuleId id,
        string code,
        int sectionNumber,
        string name,
        string? description = null,
        DateTime? lastChangedOn = null,
        bool isActive = true)
    {
        return new DataCollectionFormModule(
            id,
            code,
            sectionNumber,
            name,
            description,
            lastChangedOn ?? DateTime.UtcNow,
            isActive);
    }

    public void UpdateLastChangedOn(DateTime lastChangedOn)
    {
        LastChangedOn = lastChangedOn;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public FormField AddField(
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
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("Cannot modify a published form module.");
        }

        var field = FormField.Create(
            Id,
            fieldKey,
            label,
            fieldType,
            displayOrder,
            isRequired,
            placeholder,
            helpText,
            defaultValue,
            validationRules);

        _fields.Add(field);
        return field;
    }

    public void RemoveField(FormFieldId fieldId)
    {
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("Cannot modify a published form module.");
        }

        var field = _fields.FirstOrDefault(f => f.Id == fieldId);
        if (field != null)
        {
            _fields.Remove(field);
        }
    }

    public FormSection AddSection(
        int sectionNumber,
        string title,
        string? description = null,
        string? helpText = null,
        string? helpUrl = null)
    {
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("Cannot modify a published form module.");
        }

        var section = FormSection.Create(
            Id,
            sectionNumber,
            title,
            description,
            helpText,
            helpUrl);

        _sections.Add(section);
        return section;
    }

    public void RemoveSection(FormSectionId sectionId)
    {
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("Cannot modify a published form module.");
        }

        var section = _sections.FirstOrDefault(s => s.Id == sectionId);
        if (section != null)
        {
            _sections.Remove(section);
        }
    }

    public void Publish(UserId publishedBy)
    {
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("This form module is already published.");
        }

        if (!_fields.Any())
        {
            throw new InvalidOperationException("Cannot publish a form module with no fields.");
        }

        Status = FormStatus.Published;
        PublishedAt = DateTime.UtcNow;
        PublishedById = publishedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == FormStatus.Archived)
        {
            return;
        }

        Status = FormStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetValidationSchema(string? schema)
    {
        if (Status == FormStatus.Published)
        {
            throw new InvalidOperationException("Cannot modify a published form module.");
        }

        ValidationSchema = schema;
        UpdatedAt = DateTime.UtcNow;
    }

    public int TotalSections => _sections.Count;
}

public static class DataCollectionFormModuleCodes
{
    public const string HealthyBabies = "healthy-babies";
    public const string ServiceUsers = "service-users";
    public const string WiderServiceUsers = "wider-service-users";
    public const string OutcomeScores = "outcome-scores";
    public const string BreastfeedingRates = "breastfeeding-rates";
}