using Application.ServiceForms.Dtos;
using Application.Services.Helpers;
using Domain.ServiceForms;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Services;

// These pin the rule evaluation against a fixed "today" so the day-first regression from BLUM-7521
// is reproduced exactly rather than approximated with a date relative to the clock. The reported
// case — SMD05, value 03/10/2026, today 17/08/2026 — only fails when the value is read month-first,
// which is why a test built from DateTime.UtcNow.AddDays(...) would not have caught it.
public class ServiceFormDateAnswerValidatorTests
{
    private static readonly DateTime Today = new(2026, 8, 17);

    private static ServiceFormQuestion DateQuestion(string code, string label, string? dateValidationRule) =>
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

    private static IReadOnlyList<ServiceAnswerInputDto> Answer(string code, string value) =>
        [new ServiceAnswerInputDto(code, value)];

    [Fact]
    public void ShouldAcceptPlannedDate_WhenDayFirstValueIsInTheFuture()
    {
        // Arrange — BLUM-7521: 03/10/2026 is 3 October 2026, which is after 17 August 2026
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", ServiceFormDateValidationRules.NotInPast),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "03/10/2026"), questions, Today);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ShouldRejectPlannedDate_WhenDayFirstValueIsInThePast()
    {
        // Arrange — 10/03/2026 is 10 March 2026, genuinely before 17 August 2026
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", ServiceFormDateValidationRules.NotInPast),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "10/03/2026"), questions, Today);

        // Assert
        error.Should().Be("'When is the service expected to start?' must not be a date in the past.");
    }

    [Fact]
    public void ShouldRejectDateClosed_WhenDayFirstValueIsInTheFuture()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD04", "When did the service close?", ServiceFormDateValidationRules.NotInFuture),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD04", "03/10/2026"), questions, Today);

        // Assert
        error.Should().Be("'When did the service close?' must not be a date in the future.");
    }

    [Fact]
    public void ShouldAcceptDateClosed_WhenDayFirstValueIsInThePast()
    {
        // Arrange
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD04", "When did the service close?", ServiceFormDateValidationRules.NotInFuture),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD04", "10/03/2026"), questions, Today);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ShouldAcceptPlannedDate_WhenValueIsToday()
    {
        // Arrange - "not in past" includes today itself
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", ServiceFormDateValidationRules.NotInPast),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "17/08/2026"), questions, Today);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ShouldEvaluateIsoValues_TheSameAsDayFirstValues()
    {
        // Arrange - bulk uploads and older rows carry ISO, which must keep working
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", ServiceFormDateValidationRules.NotInPast),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "2026-10-03"), questions, Today);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ShouldSkipValidation_WhenValueIsNotAParseableDate()
    {
        // Arrange - unparseable values are left to the question's own required/format handling
        // rather than being reported here, so previously saved rows are not rejected on re-save
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", ServiceFormDateValidationRules.NotInPast),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "not a date"), questions, Today);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ShouldNotValidate_WhenQuestionHasNoDateRuleConfigured()
    {
        // Arrange - proves the rule stays per-question and is not inferred from the question code
        var questions = new List<ServiceFormQuestion>
        {
            DateQuestion("SMD05", "When is the service expected to start?", null),
        };

        // Act
        var error = ServiceFormDateAnswerValidator.ValidateAnswers(Answer("SMD05", "10/03/2026"), questions, Today);

        // Assert
        error.Should().BeNull();
    }
}