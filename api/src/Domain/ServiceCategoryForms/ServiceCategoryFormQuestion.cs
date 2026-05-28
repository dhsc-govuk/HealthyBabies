namespace Domain.ServiceCategoryForms;

public class ServiceCategoryFormQuestion : AuditableEntity<ServiceCategoryFormQuestionId>
{
    public string Code { get; private set; }
    public string Label { get; private set; }
    public string? Hint { get; private set; }
    public string? Placeholder { get; private set; }
    public ServiceCategoryFormQuestionType QuestionType { get; private set; }
    public int Step { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsPredefined { get; private set; }
    public string? HelpTextSummary { get; private set; }
    public string? HelpText { get; private set; }
    public string? ConditionalQuestionCode { get; private set; }
    public string? ConditionalValue { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<ServiceCategoryFormQuestionOption> _options = [];
    public IReadOnlyCollection<ServiceCategoryFormQuestionOption> Options => _options.AsReadOnly();

    private ServiceCategoryFormQuestion()
    {
    }

    private ServiceCategoryFormQuestion(
        ServiceCategoryFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        bool isRequired,
        bool isPredefined)
    {
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
        IsActive = true;
    }

    public static ServiceCategoryFormQuestion New(
        ServiceCategoryFormQuestionId id,
        string code,
        string label,
        string? hint,
        string? placeholder,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        bool isRequired,
        bool isPredefined)
        => new(id, code, label, hint, placeholder, questionType, step, displayOrder, isRequired, isPredefined);

    public void AddOption(string value, string label, int displayOrder)
    {
        var option = ServiceCategoryFormQuestionOption.New(
            ServiceCategoryFormQuestionOptionId.New(),
            Id,
            value,
            label,
            displayOrder);
        _options.Add(option);
    }

    public void SetConditional(string questionCode, string value)
    {
        ConditionalQuestionCode = questionCode;
        ConditionalValue = value;
    }

    public void SetHelpText(string summary, string text)
    {
        HelpTextSummary = summary;
        HelpText = text;
    }

    public void UpdateDetails(
        string label,
        string? hint,
        string? placeholder,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        bool isRequired,
        bool isPredefined,
        string? helpTextSummary,
        string? helpText,
        string? conditionalQuestionCode,
        string? conditionalValue)
    {
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
    }

    public void ClearOptions()
    {
        _options.Clear();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}