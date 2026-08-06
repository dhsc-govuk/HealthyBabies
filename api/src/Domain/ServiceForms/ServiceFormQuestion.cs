using Domain.Common;

namespace Domain.ServiceForms;

public class ServiceFormQuestion : AuditableEntity<ServiceFormQuestionId>
{
    private readonly List<ServiceFormQuestionOption> _options = new();

    public string Code { get; private set; }
    public string Label { get; private set; }
    public string? Hint { get; private set; }
    public string? Placeholder { get; private set; }
    public ServiceFormQuestionType QuestionType { get; private set; }
    public int Step { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsPredefined { get; private set; }
    public string? HelpTextSummary { get; private set; }
    public string? HelpText { get; private set; }
    public string? ConditionalQuestionCode { get; private set; }
    public string? ConditionalValue { get; private set; }

    // Rich multi-condition visibility rules (showWhen/allOf) mirroring the Data Collection form fields.
    // When set, this takes precedence over the legacy ConditionalQuestionCode/ConditionalValue pair.
    public string? ConditionalRules { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? DateValidationRule { get; private set; }

    public IReadOnlyCollection<ServiceFormQuestionOption> Options => _options.AsReadOnly();

    private ServiceFormQuestion()
    {
        Code = string.Empty;
        Label = string.Empty;
    }

    private ServiceFormQuestion(
        ServiceFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        ServiceFormQuestionType questionType,
        int step,
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
        Guard.GreaterThan(step, 0, nameof(step));
        Guard.LessThanOrEqual(step, 2, nameof(step));

        Id = id;
        Code = code;
        Label = label;
        Hint = hint;
        Placeholder = placeholder;
        QuestionType = questionType;
        Step = step;
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

    public static ServiceFormQuestion New(
        ServiceFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        ServiceFormQuestionType questionType,
        int step,
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
        return new ServiceFormQuestion(
            id,
            code,
            label,
            hint,
            placeholder,
            questionType,
            step,
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
        ServiceFormQuestionType questionType,
        int step,
        int displayOrder,
        bool isRequired,
        bool isPredefined,
        string? helpTextSummary,
        string? helpText,
        string? conditionalQuestionCode,
        string? conditionalValue,
        string? dateValidationRule = null,
        string? conditionalRules = null)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));
        Guard.GreaterThan(step, 0, nameof(step));
        Guard.LessThanOrEqual(step, 2, nameof(step));

        Label = label;
        Hint = hint;
        Placeholder = placeholder;
        QuestionType = questionType;
        Step = step;
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

    public ServiceFormQuestionOption AddOption(string value, string label, int displayOrder)
    {
        var option = ServiceFormQuestionOption.Create(Id, value, label, displayOrder);
        _options.Add(option);
        return option;
    }

    public void RemoveOption(ServiceFormQuestionOptionId optionId)
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
        Guard.GreaterThan(displayOrder, 0, nameof(displayOrder));
        DisplayOrder = displayOrder;
    }
}