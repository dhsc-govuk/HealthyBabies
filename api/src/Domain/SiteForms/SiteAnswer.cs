using Domain.Common;
using Domain.Locations;

namespace Domain.SiteForms;

public class SiteAnswer : AuditableEntity<SiteAnswerId>
{
    public LocationId LocationId { get; private set; }
    public Location? Location { get; private set; }
    public string QuestionCode { get; private set; }
    public string QuestionLabel { get; private set; }
    public string? QuestionHint { get; private set; }
    public SiteFormQuestionType QuestionType { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Value { get; private set; }
    public string? DisplayValue { get; private set; }
    public string? OptionsSnapshot { get; private set; }

    private SiteAnswer()
    {
        LocationId = LocationId.Empty();
        QuestionCode = string.Empty;
        QuestionLabel = string.Empty;
    }

    private SiteAnswer(
        SiteAnswerId id,
        LocationId locationId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        SiteFormQuestionType questionType,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(questionCode, nameof(questionCode));
        Guard.NotNullOrEmptyOrWhiteSpace(questionLabel, nameof(questionLabel));

        Id = id;
        LocationId = locationId;
        QuestionCode = questionCode;
        QuestionLabel = questionLabel;
        QuestionHint = questionHint;
        QuestionType = questionType;
        DisplayOrder = displayOrder;
        Value = value;
        DisplayValue = displayValue;
        OptionsSnapshot = optionsSnapshot;
    }

    public static SiteAnswer New(
        SiteAnswerId id,
        LocationId locationId,
        string questionCode,
        string questionLabel,
        string? questionHint,
        SiteFormQuestionType questionType,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot = null)
    {
        return new SiteAnswer(
            id,
            locationId,
            questionCode,
            questionLabel,
            questionHint,
            questionType,
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
        SiteFormQuestionType questionType,
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