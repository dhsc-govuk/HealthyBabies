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
    public DataCollectionType Type { get; private set; }

    public FormStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public UserId? PublishedById { get; private set; }
    public User? PublishedBy { get; private set; }
    public string? ValidationSchema { get; private set; }
    public bool AllowsMultipleActivities { get; private set; }

    public IReadOnlyCollection<FormSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<FormField> Fields => _fields.AsReadOnly();

    private DataCollectionFormModule()
    {
        Code = string.Empty;
        Name = string.Empty;
        Status = FormStatus.Draft;
        Type = DataCollectionType.DeliveryPlan;
    }

    private DataCollectionFormModule(
        DataCollectionFormModuleId id,
        string code,
        int sectionNumber,
        string name,
        string? description,
        DateTime lastChangedOn,
        bool isActive,
        DataCollectionType type,
        bool allowsMultipleActivities)
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
        Type = type;
        AllowsMultipleActivities = allowsMultipleActivities;
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
        bool isActive = true,
        DataCollectionType? type = null,
        bool allowsMultipleActivities = false)
    {
        return new DataCollectionFormModule(
            id,
            code,
            sectionNumber,
            name,
            description,
            lastChangedOn ?? DateTime.UtcNow,
            isActive,
            type ?? DataCollectionType.DeliveryPlan,
            allowsMultipleActivities);
    }

    public void SetAllowsMultipleActivities(bool value)
    {
        AllowsMultipleActivities = value;
        UpdatedAt = DateTime.UtcNow;
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

    public void UpdateType(DataCollectionType type)
    {
        Type = type;
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
    public const string Section1 = "section-1";
    public const string Section2 = "section-2";
    public const string Section3 = "section-3";
    public const string Section4 = "section-4";
    public const string Section5 = "section-5";
    public const string WorkforceTraining = "workforce-training";
    public const string FundedTotalWorkforce = "funded-total-workforce";
    public const string Section8 = "section-8";
    public const string Section9 = "section-9";

    // Delivery Plan modules (Focus onwards, sections 1–9)
    public const string Focus = "focus-2026-2027";
    public const string DeliveryGrant = "delivery-grant";
    public const string Parenting = "parenting";
    public const string Hle = "hle";
    public const string Send = "send";
    public const string PnmhAndPir = "pnmh-and-pir";
    public const string InfantFeeding = "infant-feeding";
    public const string HealthyBabiesOffer = "healthy-babies-offer";
    public const string RolloverSpend = "rollover-spend";
}