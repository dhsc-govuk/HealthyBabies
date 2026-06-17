using System.Net;
using System.Net.Http.Json;
using Application.SiteForms.Dtos;
using Domain.SiteForms;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.SiteForms;

public class SiteFormQuestionsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string BaseRoute = "site-form-questions";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    private static string? B64N(string? value) => value is not null ? "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)) : null;

    private readonly IReadOnlyList<SiteFormQuestion> _predefinedQuestions = SiteFormQuestionsData.GetPredefinedQuestions();

    [Fact]
    public async Task ShouldGetAllActiveQuestions()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var questions = await response.ToResponseModel<List<SiteFormQuestionDto>>();
        questions.Should().NotBeNull();
        questions.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ShouldCreateQuestion()
    {
        // Arrange
        var request = new CreateSiteFormQuestionDto(
            Code: "TEST01",
            Label: B64("Test Question"),
            Hint: B64("This is a test hint"),
            Placeholder: null,
            QuestionType: (int)SiteFormQuestionType.Text,
            IsRequired: true,
            HelpTextSummary: null,
            HelpText: null,
            ConditionalQuestionCode: null,
            ConditionalValue: null,
            Options: new List<SiteFormQuestionOptionInputDto>());

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var question = await response.ToResponseModel<SiteFormQuestionDto>();
        question.Should().NotBeNull();
        question.Code.Should().Be("TEST01");
        question.Label.Should().Be("Test Question");
        Context.SiteFormQuestions.Any(x => x.Id == new SiteFormQuestionId(question.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateQuestionBecauseCodeDuplication()
    {
        // Arrange - use existing predefined code
        var request = new CreateSiteFormQuestionDto(
            Code: "FHS01",
            Label: B64("Duplicate Code Question"),
            Hint: null,
            Placeholder: null,
            QuestionType: (int)SiteFormQuestionType.Text,
            IsRequired: false,
            HelpTextSummary: null,
            HelpText: null,
            ConditionalQuestionCode: null,
            ConditionalValue: null,
            Options: new List<SiteFormQuestionOptionInputDto>());

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldUpdateQuestion()
    {
        // Arrange - get actual question from database
        var existingQuestion = Context.SiteFormQuestions.First();
        const string newLabel = "Updated Label";
        var request = new UpdateSiteFormQuestionDto(
            Label: B64(newLabel),
            Hint: B64N(existingQuestion.Hint),
            Placeholder: B64N(existingQuestion.Placeholder),
            QuestionType: (int)existingQuestion.QuestionType,
            DisplayOrder: existingQuestion.DisplayOrder,
            IsRequired: existingQuestion.IsRequired,
            IsActive: true,
            HelpTextSummary: B64N(existingQuestion.HelpTextSummary),
            HelpText: B64N(existingQuestion.HelpText),
            ConditionalQuestionCode: existingQuestion.ConditionalQuestionCode,
            ConditionalValue: B64N(existingQuestion.ConditionalValue),
            Options: new List<SiteFormQuestionOptionInputDto>());

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{existingQuestion.Id.Value}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var question = await response.ToResponseModel<SiteFormQuestionDto>();
        question.Label.Should().Be(newLabel);
    }

    [Fact]
    public async Task ShouldDeleteQuestion()
    {
        // Arrange - create a question to delete
        var questionToDelete = SiteFormQuestion.New(
            id: SiteFormQuestionId.New(),
            code: "DELETE01",
            label: "Question to delete",
            hint: null,
            placeholder: null,
            questionType: SiteFormQuestionType.Text,
            displayOrder: 100,
            isRequired: false);

        Context.SiteFormQuestions.Add(questionToDelete);
        await SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{questionToDelete.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Context.SiteFormQuestions.Any(x => x.Id == questionToDelete.Id).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldNotDeletePredefinedQuestion()
    {
        // Arrange - get actual predefined question from database
        var predefinedQuestion = Context.SiteFormQuestions.First(q => q.IsPredefined);

        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{predefinedQuestion.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldCreateQuestionWithOptions()
    {
        // Arrange
        var request = new CreateSiteFormQuestionDto(
            Code: "RADIO01",
            Label: B64("Radio Question"),
            Hint: B64("Select one option"),
            Placeholder: null,
            QuestionType: (int)SiteFormQuestionType.Radio,
            IsRequired: true,
            HelpTextSummary: null,
            HelpText: null,
            ConditionalQuestionCode: null,
            ConditionalValue: null,
            Options: new List<SiteFormQuestionOptionInputDto>
            {
                new(B64("yes"), B64("Yes"), 1),
                new(B64("no"), B64("No"), 2)
            });

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var question = await response.ToResponseModel<SiteFormQuestionDto>();
        question.Options.Should().HaveCount(2);
    }

    public async Task InitializeAsync()
    {
        if (!Context.SiteFormQuestions.Any())
        {
            Context.SiteFormQuestions.AddRange(_predefinedQuestions);
            await SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}