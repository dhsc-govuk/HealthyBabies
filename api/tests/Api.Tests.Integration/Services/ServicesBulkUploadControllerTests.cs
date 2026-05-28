using System.Net.Http.Json;
using Api.Controllers;
using Application.Services.Commands;
using Application.Services.Queries;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Services;

public class ServicesBulkUploadControllerTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private Service _service1 = null!;
    private Service _service2 = null!;
    private Service _serviceWithAnswers = null!;

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    [Fact]
    public async Task ShouldDownloadTemplate_ReturnsCsvWithQuestionCodes()
    {
        // Act
        var response = await Client.GetAsync("services/bulk-upload/template");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First row should contain question codes
        lines[0].Should().Contain("SMD01");
        lines[0].Should().Contain("SMD02");
        lines[0].Should().Contain("SMD14");

        // Second row should contain labels
        lines[1].Should().Contain("What is the service name?");
        lines[1].Should().Contain("Is the service funded by the BSFH&HB programme?");
        lines[1].Should().Contain("Is this a targeted, specialist or universal service?");
    }

    [Fact]
    public async Task ShouldMatchServices_ReturnsMatchedAndUnmatchedServices()
    {
        // Arrange
        var request = new MatchServicesRequest(new List<string> { "Service One", "Non Existent Service" });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload/match", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var results = await response.ToResponseModel<List<ServiceMatchResult>>();

        results.Should().HaveCount(2);

        // Check matched service
        var matchedResult = results.First(r => r.SearchName == "Service One");
        matchedResult.ServiceId.Should().Be(_service1.Id.Value);
        matchedResult.MatchedName.Should().Be("Service One");

        // Check unmatched service
        var unmatchedResult = results.First(r => r.SearchName == "Non Existent Service");
        unmatchedResult.ServiceId.Should().BeNull();
        unmatchedResult.MatchedName.Should().BeNull();
    }

    [Fact]
    public async Task ShouldBulkUpdate_UpdatesSingleService()
    {
        // Arrange
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_service1.Id.Value, B64("Updated Service One"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Updated Service One")),
                new("SMD02", B64("0")),
                new("SMD14", B64("0"))
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.ToResponseModel<BulkUpdateServicesResult>();

        result.TotalCount.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.Results.First().IsSuccess.Should().BeTrue();

        // Verify database state
        await Context.Entry(_service1).ReloadAsync();
        _service1.Name.Should().Be("Updated Service One");
    }

    [Fact]
    public async Task ShouldBulkUpdate_UpdatesMultipleServices()
    {
        // Arrange
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_service1.Id.Value, B64("Updated Service One"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Updated Service One")),
                new("SMD02", B64("0"))
            }),
            new(_service2.Id.Value, B64("Updated Service Two"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Updated Service Two")),
                new("SMD02", B64("1"))
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.ToResponseModel<BulkUpdateServicesResult>();

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);

        // Verify database state
        await Context.Entry(_service1).ReloadAsync();
        await Context.Entry(_service2).ReloadAsync();
        _service1.Name.Should().Be("Updated Service One");
        _service2.Name.Should().Be("Updated Service Two");
    }

    [Fact]
    public async Task ShouldBulkUpdate_ClearsExistingAnswers()
    {
        // Arrange - service with existing answers
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_serviceWithAnswers.Id.Value, B64("Service With New Answers"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Service With New Answers")),
                new("SMD02", B64("1"))
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify old answers are cleared and new ones added
        var serviceAnswers = Context.ServiceAnswers
            .Where(a => a.ServiceId == _serviceWithAnswers.Id)
            .ToList();

        // Should only have the new answers (SMD01, SMD02), not the old SMD14
        serviceAnswers.Should().HaveCount(2);
        serviceAnswers.Should().Contain(a => a.QuestionCode == "SMD01");
        serviceAnswers.Should().Contain(a => a.QuestionCode == "SMD02");
        serviceAnswers.Should().NotContain(a => a.QuestionCode == "SMD14");
    }

    [Fact]
    public async Task ShouldBulkUpdate_MapsCheckboxValuesToDisplayLabels()
    {
        // Arrange
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_service1.Id.Value, B64("Service With Checkbox"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Service With Checkbox")),
                new("SMD14", B64("0,1")) // Should map to "Targeted, Specialist"
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify display value mapping
        var checkboxAnswer = Context.ServiceAnswers
            .FirstOrDefault(a => a.ServiceId == _service1.Id && a.QuestionCode == "SMD14");

        checkboxAnswer.Should().NotBeNull();
        checkboxAnswer!.DisplayValue.Should().Be("Targeted, Specialist");
    }

    [Fact]
    public async Task ShouldBulkUpdate_SetsServiceStatusToComplete()
    {
        // Arrange
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_service1.Id.Value, B64("Completed Service"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Completed Service")),
                new("SMD02", B64("0")),
                new("SMD14", B64("0"))
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify service status is Complete and step is 3
        await Context.Entry(_service1).ReloadAsync();
        _service1.Status.Should().Be(ServiceStatus.Complete);
        _service1.CurrentStep.Should().Be(3);
    }

    [Fact]
    public async Task ShouldBulkUpdate_ReturnsPartialSuccess_WhenOneServiceFails()
    {
        // Arrange
        var nonExistentServiceId = Guid.NewGuid();
        var request = new BulkUpdateServicesRequest(null, new List<BulkUpdateServiceItemRequest>
        {
            new(_service1.Id.Value, B64("Valid Service"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Valid Service")),
                new("SMD02", B64("0"))
            }),
            new(nonExistentServiceId, B64("Non Existent Service"), new List<AnswerInputRequest>
            {
                new("SMD01", B64("Non Existent Service")),
                new("SMD02", B64("0"))
            })
        });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.ToResponseModel<BulkUpdateServicesResult>();

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);

        var successResult = result.Results.First(r => r.ServiceId == _service1.Id.Value);
        successResult.IsSuccess.Should().BeTrue();

        var failedResult = result.Results.First(r => r.ServiceId == nonExistentServiceId);
        failedResult.IsSuccess.Should().BeFalse();
        failedResult.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    public async Task InitializeAsync()
    {
        await ServiceFormQuestionSeeder.SeedAsync(Context);

        // Create organisation with ID from OrganisationAdminUsersData
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        // Note: ServiceFormQuestions are already seeded by the application startup
        // (see ServiceFormQuestionSeeder.cs)

        // Create test services
        _service1 = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service One");
        Context.Services.Add(_service1);

        _service2 = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service Two");
        Context.Services.Add(_service2);

        // Create service with existing answers to test clearing
        _serviceWithAnswers = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service With Answers");
        _serviceWithAnswers.AddAnswer(
            "SMD01",
            "What is the service name?",
            null,
            ServiceFormQuestionType.Text,
            1,
            1,
            "Old Service Name",
            "Old Service Name");
        _serviceWithAnswers.AddAnswer(
            "SMD14",
            "Is this a targeted, specialist or universal service?",
            null,
            ServiceFormQuestionType.Checkbox,
            2,
            8,
            "0",
            "Targeted");
        Context.Services.Add(_serviceWithAnswers);
        _ = await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}