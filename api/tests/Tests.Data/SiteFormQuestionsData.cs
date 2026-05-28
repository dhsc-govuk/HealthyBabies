using Domain.SiteForms;

namespace Tests.Data;

public static class SiteFormQuestionsData
{
    public static SiteFormQuestion NameQuestion() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS01",
        label: "Site name",
        hint: "Enter the name of the site",
        placeholder: null,
        questionType: SiteFormQuestionType.Text,
        displayOrder: 1,
        isRequired: true,
        isPredefined: true);

    public static SiteFormQuestion PostCodeQuestion() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS02",
        label: "Post code",
        hint: "Enter the post code",
        placeholder: null,
        questionType: SiteFormQuestionType.Text,
        displayOrder: 2,
        isRequired: true,
        isPredefined: true);

    public static SiteFormQuestion AddressLine1Question() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS16",
        label: "Address line 1",
        hint: null,
        placeholder: "Enter address line 1",
        questionType: SiteFormQuestionType.Text,
        displayOrder: 3,
        isRequired: false,
        isPredefined: true);

    public static SiteFormQuestion AddressLine2Question() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS13",
        label: "Address line 2 (optional)",
        hint: null,
        placeholder: "Enter address line 2",
        questionType: SiteFormQuestionType.Text,
        displayOrder: 4,
        isRequired: false,
        isPredefined: true);

    public static SiteFormQuestion TownOrCityQuestion() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS14",
        label: "Town or city",
        hint: null,
        placeholder: "Enter town or city",
        questionType: SiteFormQuestionType.Text,
        displayOrder: 5,
        isRequired: false,
        isPredefined: true);

    public static SiteFormQuestion CountyQuestion() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS15",
        label: "County (optional)",
        hint: null,
        placeholder: "Enter county",
        questionType: SiteFormQuestionType.Text,
        displayOrder: 6,
        isRequired: false,
        isPredefined: true);

    public static SiteFormQuestion ReferenceNumberQuestion() => SiteFormQuestion.New(
        id: SiteFormQuestionId.New(),
        code: "FHS03",
        label: "Reference number",
        hint: "Enter the reference number",
        placeholder: null,
        questionType: SiteFormQuestionType.Text,
        displayOrder: 7,
        isRequired: false,
        isPredefined: true);

    public static IReadOnlyList<SiteFormQuestion> GetPredefinedQuestions() =>
    [
        NameQuestion(),
        PostCodeQuestion(),
        AddressLine1Question(),
        AddressLine2Question(),
        TownOrCityQuestion(),
        CountyQuestion(),
        ReferenceNumberQuestion()
    ];
}