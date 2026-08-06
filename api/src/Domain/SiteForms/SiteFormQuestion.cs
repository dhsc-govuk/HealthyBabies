using Domain.Common;

namespace Domain.SiteForms;

public class SiteFormQuestion : AuditableEntity<SiteFormQuestionId>
{
    private readonly List<SiteFormQuestionOption> _options = new();

    public string Code { get; private set; }
    public string Label { get; private set; }
    public string? Hint { get; private set; }
    public string? Placeholder { get; private set; }
    public SiteFormQuestionType QuestionType { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsPredefined { get; private set; }
    public string? HelpTextSummary { get; private set; }
    public string? HelpText { get; private set; }
    public string? ConditionalQuestionCode { get; private set; }
    public string? ConditionalValue { get; private set; }

    // Rich multi-condition visibility rules (showWhen/allOf); takes precedence over the legacy
    // ConditionalQuestionCode/ConditionalValue pair when set.
    public string? ConditionalRules { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? DateValidationRule { get; private set; }

    public IReadOnlyCollection<SiteFormQuestionOption> Options => _options.AsReadOnly();

    private SiteFormQuestion()
    {
        Code = string.Empty;
        Label = string.Empty;
    }

    private SiteFormQuestion(
        SiteFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        SiteFormQuestionType questionType,
        int displayOrder,
        bool isRequired,
        bool isPredefined,
        string? helpTextSummary,
        string? helpText,
        string? conditionalQuestionCode,
        string? conditionalValue,
        string? dateValidationRule,
        string? conditionalRules)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(code, nameof(code));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Id = id;
        Code = code;
        Label = label;
        Hint = hint;
        Placeholder = placeholder;
        QuestionType = questionType;
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
        IsPredefined = isPredefined;
        HelpTextSummary = helpTextSummary;
        HelpText = helpText;
        ConditionalQuestionCode = conditionalQuestionCode;
        ConditionalValue = conditionalValue;
        DateValidationRule = dateValidationRule;
        ConditionalRules = conditionalRules;
    }

    public static SiteFormQuestion New(
        SiteFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        SiteFormQuestionType questionType,
        int displayOrder,
        bool isRequired,
        bool isPredefined = false,
        string? helpTextSummary = null,
        string? helpText = null,
        string? conditionalQuestionCode = null,
        string? conditionalValue = null,
        string? dateValidationRule = null,
        string? conditionalRules = null)
    {
        return new SiteFormQuestion(
            id,
            code,
            label,
            hint,
            placeholder,
            questionType,
            displayOrder,
            isRequired,
            isPredefined,
            helpTextSummary,
            helpText,
            conditionalQuestionCode,
            conditionalValue,
            dateValidationRule,
            conditionalRules);
    }

    public void UpdateDetails(
        string label,
        string? hint,
        string? placeholder,
        SiteFormQuestionType questionType,
        int displayOrder,
        bool isRequired,
        string? helpTextSummary,
        string? helpText,
        string? conditionalQuestionCode,
        string? conditionalValue,
        string? dateValidationRule = null,
        string? conditionalRules = null)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Label = label;
        Hint = hint;
        Placeholder = placeholder;
        QuestionType = questionType;
        DisplayOrder = displayOrder;
        IsRequired = isRequired;
        HelpTextSummary = helpTextSummary;
        HelpText = helpText;
        ConditionalQuestionCode = conditionalQuestionCode;
        ConditionalValue = conditionalValue;
        DateValidationRule = dateValidationRule;
        ConditionalRules = conditionalRules;
    }

    public SiteFormQuestionOption AddOption(string value, string label, int displayOrder)
    {
        var option = SiteFormQuestionOption.Create(Id, value, label, displayOrder);
        _options.Add(option);
        return option;
    }

    public void RemoveOption(SiteFormQuestionOptionId optionId)
    {
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            _options.Remove(option);
        }
    }

    public void ClearOptions()
    {
        _options.Clear();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetDisplayOrder(int displayOrder)
    {
        Guard.GreaterThanOrEqual(displayOrder, 0, nameof(displayOrder));
        DisplayOrder = displayOrder;
    }
}