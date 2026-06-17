using Domain.Common;
using Domain.Services;

namespace Domain.ServiceForms;

public class ServiceAnswer : AuditableEntity<ServiceAnswerId>
{
    public ServiceId ServiceId { get; private set; }
    public Service? Service { get; private set; }
    public string QuestionCode { get; private set; }
    public string QuestionLabel { get; private set; }
    public string? QuestionHint { get; private set; }
    public ServiceFormQuestionType QuestionType { get; private set; }
    public int Step { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Value { get; private set; }
    public string? DisplayValue { get; private set; }
    public string? OptionsSnapshot { get; private set; }

    private ServiceAnswer()
    {
        ServiceId = ServiceId.Empty();
        QuestionCode = string.Empty;
        QuestionLabel = string.Empty;
    }

    private ServiceAnswer(
        ServiceAnswerId id,
        ServiceId serviceId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(questionCode, nameof(questionCode));
        Guard.NotNullOrEmptyOrWhiteSpace(questionLabel, nameof(questionLabel));

        Id = id;
        ServiceId = serviceId;
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

    public static ServiceAnswer New(
        ServiceAnswerId id,
        ServiceId serviceId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot = null)
    {
        return new ServiceAnswer(
            id,
            serviceId,
            questionCode,
            questionLabel,
            questionHint,
            questionType,
            step,
            displayOrder,
            value,
            displayValue,
            optionsSnapshot);
    }

    public void UpdateAnswer(string? value, string? displayValue)
    {
        Value = value;
        DisplayValue = displayValue;
    }

    public void UpdateSnapshot(
        string questionLabel,
        string? questionHint,
        ServiceFormQuestionType questionType,
        int displayOrder,
        string? optionsSnapshot)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(questionLabel, nameof(questionLabel));

        QuestionLabel = questionLabel;
        QuestionHint = questionHint;
        QuestionType = questionType;
        DisplayOrder = displayOrder;
        OptionsSnapshot = optionsSnapshot;
    }
}