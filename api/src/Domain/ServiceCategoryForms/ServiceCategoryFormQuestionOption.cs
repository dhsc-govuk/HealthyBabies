namespace Domain.ServiceCategoryForms;

public class ServiceCategoryFormQuestionOption : AuditableEntity<ServiceCategoryFormQuestionOptionId>
{
    public ServiceCategoryFormQuestionId QuestionId { get; private set; }
    public ServiceCategoryFormQuestion? Question { get; private set; }
    public string Value { get; private set; }
    public string Label { get; private set; }
    public int DisplayOrder { get; private set; }

    private ServiceCategoryFormQuestionOption()
    {
    }

    private ServiceCategoryFormQuestionOption(
        ServiceCategoryFormQuestionOptionId id,
        ServiceCategoryFormQuestionId questionId,
        string value,
        string label,
        int displayOrder)
    {
        Id = id;
        QuestionId = questionId;
        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
    }

    public static ServiceCategoryFormQuestionOption New(
        ServiceCategoryFormQuestionOptionId id,
        ServiceCategoryFormQuestionId questionId,
        string value,
        string label,
        int displayOrder)
        => new(id, questionId, value, label, displayOrder);
}