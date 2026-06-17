using Domain.ServiceCategories;

namespace Domain.ServiceCategoryForms;

public class ServiceCategoryAnswer : AuditableEntity<ServiceCategoryAnswerId>
{
    public ServiceCategoryId ServiceCategoryId { get; private set; }
    public ServiceCategory? ServiceCategory { get; private set; }
    public string QuestionCode { get; private set; }
    public string QuestionLabel { get; private set; }
    public string? QuestionHint { get; private set; }
    public ServiceCategoryFormQuestionType QuestionType { get; private set; }
    public int Step { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Value { get; private set; }
    public string? DisplayValue { get; private set; }
    public string? OptionsSnapshot { get; private set; }

    private ServiceCategoryAnswer()
    {
    }

    private ServiceCategoryAnswer(
        ServiceCategoryAnswerId id,
        ServiceCategoryId serviceCategoryId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot)
    {
        Id = id;
        ServiceCategoryId = serviceCategoryId;
        QuestionCode = questionCode;
        QuestionLabel = questionLabel;
        QuestionHint = questionHint;
        QuestionType = questionType;
        Step = step;
        DisplayOrder = displayOrder;
        Value = value;
        DisplayValue = displayValue;
        OptionsSnapshot = optionsSnapshot;
    }

    public static ServiceCategoryAnswer New(
        ServiceCategoryAnswerId id,
        ServiceCategoryId serviceCategoryId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot)
        => new(id, serviceCategoryId, questionCode, questionLabel, questionHint, questionType, step, displayOrder, value, displayValue, optionsSnapshot);

    public void UpdateAnswer(string? value, string? displayValue)
    {
        Value = value;
        DisplayValue = displayValue;
    }

    public void UpdateSnapshot(
        string questionLabel,
        string? questionHint,
        ServiceCategoryFormQuestionType questionType,
        int displayOrder,
        string? optionsSnapshot)
    {
        QuestionLabel = questionLabel;
        QuestionHint = questionHint;
        QuestionType = questionType;
        DisplayOrder = displayOrder;
        OptionsSnapshot = optionsSnapshot;
    }
}