using Domain.Common;

namespace Domain.ServiceForms;

public class ServiceFormQuestionOption : AuditableEntity<ServiceFormQuestionOptionId>
{
    public ServiceFormQuestionId QuestionId { get; private set; }
    public ServiceFormQuestion? Question { get; private set; }
    public string Value { get; private set; }
    public string Label { get; private set; }
    public int DisplayOrder { get; private set; }

    private ServiceFormQuestionOption()
    {
        QuestionId = ServiceFormQuestionId.Empty();
        Value = string.Empty;
        Label = string.Empty;
    }

    private ServiceFormQuestionOption(
        ServiceFormQuestionOptionId id,
        ServiceFormQuestionId questionId,
        string value,
        string label,
        int displayOrder)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Id = id;
        QuestionId = questionId;
        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
    }

    internal static ServiceFormQuestionOption Create(
        ServiceFormQuestionId questionId,
        string value,
        string label,
        int displayOrder)
    {
        return new ServiceFormQuestionOption(
            ServiceFormQuestionOptionId.New(),
            questionId,
            value,
            label,
            displayOrder);
    }

    public void UpdateDetails(string value, string label, int displayOrder)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
    }
}