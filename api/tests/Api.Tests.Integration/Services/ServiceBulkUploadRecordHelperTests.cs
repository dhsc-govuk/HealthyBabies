using Application.Services.Helpers;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Services;

public class ServiceBulkUploadRecordHelperTests
{
    private static ServiceFormQuestion DateQuestion(string code, string label, string dateValidationRule) =>
        ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            code,
            label,
            null,
            null,
            ServiceFormQuestionType.Date,
            step: 1,
            displayOrder: 1,
            isRequired: false,
            dateValidationRule: dateValidationRule);

    private static ServiceFormQuestion AgeQuestion(string code, string label)
    {
        var question = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            code,
            label,
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 1,
            isRequired: false);
        question.AddOption("0_2", "0 - 2", 1);
        question.AddOption("3_4", "3 - 4", 2);
        question.AddOption("18_25", "18 - 25", 3);
        question.AddOption("unknown", "Unknown", 4);
        return question;
    }

    private static ServiceFormQuestion ConditionalQuestion(
        string code,
        int displayOrder,
        ServiceFormQuestionType questionType,
        string? conditionalQuestionCode = null,
        string? conditionalValue = null) =>
        ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            code,
            $"{code} label",
            null,
            null,
            questionType,
            step: 2,
            displayOrder: displayOrder,
            isRequired: true,
            isPredefined: false,
            helpText: null,
            conditionalQuestionCode: conditionalQuestionCode,
            conditionalValue: conditionalValue);

    // SMD08 (strand) -> SMD10 (category, checkbox) -> SMD10A (exemption). Mirrors the production chain
    // behind BLUM-7221.
    private static List<ServiceFormQuestion> StrandChainQuestions() =>
    [
        ConditionalQuestion("SMD08", 3, ServiceFormQuestionType.Radio),
        ConditionalQuestion("SMD10", 5, ServiceFormQuestionType.Checkbox, "SMD08", "parenting"),
        ConditionalQuestion("SMD10A", 6, ServiceFormQuestionType.Radio, "SMD10", "exemption_other"),
    ];

    [Fact]
    public void ShouldNotRequireGrandchild_WhenGrandparentNoLongerSelectsTheBranch()
    {
        // Arrange - the strand moved off parenting, so SMD10 no longer applies. Its leftover cell must
        // not keep SMD10A alive (BLUM-7221).
        var record = new Dictionary<string, string>
        {
            ["SMD08"] = "infant_feeding",
            ["SMD10"] = "exemption_other",
        };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, StrandChainQuestions());

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ShouldRequireGrandchild_WhenTheWholeChainHolds()
    {
        // Arrange
        var record = new Dictionary<string, string>
        {
            ["SMD08"] = "parenting",
            ["SMD10"] = "exemption_other",
        };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, StrandChainQuestions());

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD10A");
    }

    [Fact]
    public void ShouldRequireGrandchild_WhenCheckboxParentSelectsTheTriggerAlongsideOthers()
    {
        // Arrange - checkbox answers are comma separated, so an exact string comparison against the
        // conditional value would wrongly treat this parent as unselected.
        var record = new Dictionary<string, string>
        {
            ["SMD08"] = "parenting",
            ["SMD10"] = "child_parent_psychotherapy,exemption_other",
        };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, StrandChainQuestions());

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD10A");
    }

    [Fact]
    public void ShouldNotPersistAnswers_ForQuestionsHiddenByAChangedAncestor()
    {
        // Arrange
        var service = Service.New(ServiceId.New(), OrganisationId.New(), "Bulk Uploaded Service");
        var record = new Dictionary<string, string>
        {
            ["SMD08"] = "infant_feeding",
            ["SMD10"] = "exemption_other",
            ["SMD10A"] = "mellow_toddlers",
        };

        // Act
        ServiceBulkUploadRecordHelper.ApplyAnswers(service, record, StrandChainQuestions());

        // Assert
        service.Answers.Should().ContainSingle(a => a.QuestionCode == "SMD08");
        service.Answers.Should().NotContain(a => a.QuestionCode == "SMD10");
        service.Answers.Should().NotContain(a => a.QuestionCode == "SMD10A");
    }

    [Fact]
    public void ShouldMatchConditionalValuesCaseInsensitively_AsOptionValidationDoes()
    {
        // Arrange - option validation accepts "Parenting" for the option "parenting", so no error is
        // raised anywhere. The conditional has to agree, or the whole branch is silently discarded.
        var service = Service.New(ServiceId.New(), OrganisationId.New(), "Mixed Case Service");
        var record = new Dictionary<string, string>
        {
            ["SMD08"] = "Parenting",
            ["SMD10"] = "Exemption_Other",
            ["SMD10A"] = "mellow_toddlers",
        };

        // Act
        ServiceBulkUploadRecordHelper.ApplyAnswers(service, record, StrandChainQuestions());

        // Assert
        service.Answers.Should().Contain(a => a.QuestionCode == "SMD10");
        service.Answers.Should().Contain(a => a.QuestionCode == "SMD10A");
    }

    [Fact]
    public void ShouldReturnError_WhenDateClosedIsInTheFuture()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD04", "Date closed", ServiceFormDateValidationRules.NotInFuture) };
        var record = new Dictionary<string, string> { ["SMD04"] = DateTime.UtcNow.Date.AddDays(10).ToString("yyyy-MM-dd") };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD04" && e.Message.Contains("must not be a date in the future"));
    }

    [Fact]
    public void ShouldReturnError_WhenPlannedDateIsInThePast()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD05", "Planned date", ServiceFormDateValidationRules.NotInPast) };
        var record = new Dictionary<string, string> { ["SMD05"] = DateTime.UtcNow.Date.AddDays(-10).ToString("yyyy-MM-dd") };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD05" && e.Message.Contains("must not be a date in the past"));
    }

    [Fact]
    public void ShouldNotReturnError_WhenDateQuestionHasNoValidationRuleConfigured()
    {
        // Arrange - proves the rule is genuinely per-question/configurable, not hardcoded by question code
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD99", "Some other date question", null!) };
        var record = new Dictionary<string, string> { ["SMD99"] = DateTime.UtcNow.Date.AddDays(10).ToString("yyyy-MM-dd") };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().BeEmpty();
    }

    // The date tests above use ISO, the format the upload template advertises. Cells are read as
    // their displayed text though, so day-first and long-form dates reach here too and must be
    // read day-first rather than month-first (BLUM-7521).
    [Fact]
    public void ShouldReturnError_WhenPlannedDateIsInThePast_GivenDayFirstFormat()
    {
        // Arrange - day 20 has no month-first reading, so the old parse failed and reported a
        // format error instead of evaluating the rule
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD05", "Planned date", ServiceFormDateValidationRules.NotInPast) };
        var record = new Dictionary<string, string> { ["SMD05"] = "20/01/2020" };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD05" && e.Message.Contains("must not be a date in the past"));
    }

    [Fact]
    public void ShouldNotReturnError_WhenDateClosedIsInThePast_GivenLongFormDate()
    {
        // Arrange - an Excel column formatted as a long date arrives spelled out
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD04", "Date closed", ServiceFormDateValidationRules.NotInFuture) };
        var record = new Dictionary<string, string> { ["SMD04"] = "9 September 2020" };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnFormatError_WhenDateValueIsNotADate()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion> { DateQuestion("SMD04", "Date closed", ServiceFormDateValidationRules.NotInFuture) };
        var record = new Dictionary<string, string> { ["SMD04"] = "not a date" };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD04" && e.Message == "Invalid date format. Expected: YYYY-MM-DD or DD/MM/YYYY");
    }

    [Fact]
    public void ShouldReturnError_WhenLowestAgeIsHigherThanHighestAge()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion>
        {
            AgeQuestion("SMD13", "Lowest age"),
            AgeQuestion("SMD14", "Highest age"),
        };
        var record = new Dictionary<string, string> { ["SMD13"] = "18_25", ["SMD14"] = "3_4" };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().ContainSingle(e => e.QuestionCode == "SMD14" && e.Message.Contains("cannot be lower than"));
    }

    [Fact]
    public void ShouldNotReturnError_WhenLowestAndHighestAgeAreEqual()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion>
        {
            AgeQuestion("SMD13", "Lowest age"),
            AgeQuestion("SMD14", "Highest age"),
        };
        var record = new Dictionary<string, string> { ["SMD13"] = "18_25", ["SMD14"] = "18_25" };

        // Act
        var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);

        // Assert
        errors.Should().BeEmpty();
    }
}