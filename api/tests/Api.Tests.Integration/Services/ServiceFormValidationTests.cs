using System.Net;
using System.Net.Http.Json;
using Api.Controllers;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Services;

// The code-based fallback seeder used in this test environment models SMD04/SMD05/SMD13/SMD14 differently
// to the exported production form schema this validation targets (see ServiceFormQuestionSeeder.cs vs.
// api/src/Api/SeedData/seeder-data-v3.json). Each test reshapes the seeded rows to match the production
// schema (SMD04/SMD05 = Date, SMD13/SMD14 = Radio over a shared age-bracket option set) so the new
// validators are exercised the way they behave against the real deployed question set.
public class ServiceFormValidationTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private Service _service = null!;

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        _service = ServicesData.MainService(OrganisationAdminUsersData.OrganisationId);
        Context.Services.Add(_service);

        var smd04 = await Context.ServiceFormQuestions.SingleAsync(q => q.Code == "SMD04");
        smd04.UpdateDetails(
            label: "When did the service close?",
            hint: null,
            placeholder: null,
            questionType: ServiceFormQuestionType.Date,
            step: 1,
            displayOrder: smd04.DisplayOrder,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: null,
            helpText: null,
            conditionalQuestionCode: "SMD03",
            conditionalValue: "2",
            dateValidationRule: ServiceFormDateValidationRules.NotInFuture);

        var smd05 = await Context.ServiceFormQuestions.Include(q => q.Options).SingleAsync(q => q.Code == "SMD05");
        smd05.UpdateDetails(
            label: "When is the service expected to start?",
            hint: null,
            placeholder: null,
            questionType: ServiceFormQuestionType.Date,
            step: 1,
            displayOrder: smd05.DisplayOrder,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: null,
            helpText: null,
            conditionalQuestionCode: "SMD03",
            conditionalValue: "1",
            dateValidationRule: ServiceFormDateValidationRules.NotInPast);
        smd05.ClearOptions();

        var smd13 = await Context.ServiceFormQuestions.Include(q => q.Options).SingleAsync(q => q.Code == "SMD13");
        smd13.UpdateDetails(
            label: "What is the lowest age of children expected to benefit from the service?",
            hint: null,
            placeholder: null,
            questionType: ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: smd13.DisplayOrder,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: null,
            helpText: null,
            conditionalQuestionCode: null,
            conditionalValue: null);
        smd13.ClearOptions();
        AddAgeOptions(smd13);

        var smd14 = await Context.ServiceFormQuestions.Include(q => q.Options).SingleAsync(q => q.Code == "SMD14");
        smd14.UpdateDetails(
            label: "What is the highest age of children expected to benefit from the service?",
            hint: null,
            placeholder: null,
            questionType: ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: smd14.DisplayOrder,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: null,
            helpText: "Highest age cannot be lower than lowest age.",
            conditionalQuestionCode: null,
            conditionalValue: null);
        smd14.ClearOptions();
        AddAgeOptions(smd14);

        await SaveChangesAsync();
    }

    private static void AddAgeOptions(ServiceFormQuestion question)
    {
        question.AddOption("up_to_birth", "Up to birth", 1);
        question.AddOption("0_2", "0 - 2", 2);
        question.AddOption("3_4", "3 - 4", 3);
        question.AddOption("5_11", "5 - 11", 4);
        question.AddOption("12_17", "12 - 17", 5);
        question.AddOption("18_25", "18 - 25", 6);
        question.AddOption("26_plus", "26 and older", 7);
        question.AddOption("unknown", "Unknown", 8);
    }

    [Fact]
    public async Task ShouldRejectStepOne_WhenDateClosedIsInTheFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.Date.AddDays(30).ToString("yyyy-MM-dd");
        var request = new UpdateServiceStepOneRequest(
            Name: B64(_service.Name),
            Answers: [new AnswerInputRequest("SMD04", B64(futureDate))],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-one", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("must not be a date in the future");
    }

    [Fact]
    public async Task ShouldAcceptStepOne_WhenDateClosedIsInThePast()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.Date.AddDays(-30).ToString("yyyy-MM-dd");
        var request = new UpdateServiceStepOneRequest(
            Name: B64(_service.Name),
            Answers: [new AnswerInputRequest("SMD04", B64(pastDate))],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShouldRejectStepOne_WhenPlannedDateIsInThePast()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.Date.AddDays(-30).ToString("yyyy-MM-dd");
        var request = new UpdateServiceStepOneRequest(
            Name: B64(_service.Name),
            Answers: [new AnswerInputRequest("SMD05", B64(pastDate))],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-one", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("must not be a date in the past");
    }

    [Fact]
    public async Task ShouldAcceptStepOne_WhenPlannedDateIsInTheFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.Date.AddDays(30).ToString("yyyy-MM-dd");
        var request = new UpdateServiceStepOneRequest(
            Name: B64(_service.Name),
            Answers: [new AnswerInputRequest("SMD05", B64(futureDate))],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShouldRejectStepTwo_WhenLowestAgeIsHigherThanHighestAge()
    {
        // Arrange
        var request = new UpdateServiceStepTwoRequest(
            Answers:
            [
                new AnswerInputRequest("SMD13", B64("18_25")),
                new AnswerInputRequest("SMD14", B64("3_4")),
            ],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-two", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("cannot be lower than");
    }

    [Fact]
    public async Task ShouldAcceptStepTwo_WhenLowestAndHighestAgeAreEqual()
    {
        // Arrange - a single-age cohort (e.g. 18-25 for both) must be accepted
        var request = new UpdateServiceStepTwoRequest(
            Answers:
            [
                new AnswerInputRequest("SMD13", B64("18_25")),
                new AnswerInputRequest("SMD14", B64("18_25")),
            ],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-two", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShouldAcceptStepTwo_WhenHighestAgeIsUnknown()
    {
        // Arrange - "unknown" isn't an ordinal age bracket, so it can't be cross-checked
        var request = new UpdateServiceStepTwoRequest(
            Answers:
            [
                new AnswerInputRequest("SMD13", B64("18_25")),
                new AnswerInputRequest("SMD14", B64("unknown")),
            ],
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_service.Id.Value}/step-two", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}