using Application.Services.Helpers;
using Domain.ServiceForms;
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