using Domain.Common;

namespace Domain.SiteForms;

public class SiteFormQuestionOption : AuditableEntity<SiteFormQuestionOptionId>
{
    public SiteFormQuestionId QuestionId { get; private set; }
    public SiteFormQuestion? Question { get; private set; }
    public string Value { get; private set; }
    public string Label { get; private set; }
    public int DisplayOrder { get; private set; }

    private SiteFormQuestionOption()
    {
        QuestionId = SiteFormQuestionId.Empty();
        Value = string.Empty;
        Label = string.Empty;
    }

    private SiteFormQuestionOption(
        SiteFormQuestionOptionId id,
        SiteFormQuestionId questionId,
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

    internal static SiteFormQuestionOption Create(
        SiteFormQuestionId questionId,
        string value,
        string label,
        int displayOrder)
    {
        return new SiteFormQuestionOption(
            SiteFormQuestionOptionId.New(),
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