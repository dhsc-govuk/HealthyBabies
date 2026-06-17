using System.Net;
using System.Net.Http.Json;
using Api.Controllers;
using Application.Services.Dtos;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Services;

public class ServicesControllerTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private Service _mainService = null!;
    private Service _serviceAtStepTwo = null!;
    private Service _completedService = null!;

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    [Fact]
    public async Task ShouldCreateService_ReturnsNewDraftService()
    {
        // Arrange
        var request = new CreateServiceRequest(B64("Test Service"));

        // Act
        var response = await Client.PostAsJsonAsync("services/create", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.Id.Should().NotBeNull();
        service.Status.Should().Be(0); // Draft
        service.CurrentStep.Should().Be(1);
        service.Name.Should().Be("Test Service");
    }

    [Fact]
    public async Task ShouldGetAllServices_ReturnsOrganisationServices()
    {
        // Act
        var response = await Client.GetAsync("services");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var services = await response.ToResponseModel<List<ServiceListDto>>();
        services.Should().Contain(s => s.Id == _mainService.Id.Value);
    }

    [Fact]
    public async Task ShouldGetServiceById_ReturnsService()
    {
        // Act
        var response = await Client.GetAsync($"services/{_mainService.Id.Value}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.Id.Should().Be(_mainService.Id.Value);
        service.Name.Should().Be(_mainService.Name);
    }

    [Fact]
    public async Task ShouldGetServiceById_ReturnsNotFound_WhenServiceDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"services/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldUpdateStepOne_UpdatesServiceNameAndAdvancesStep()
    {
        // Arrange
        var request = new UpdateServiceStepOneRequest(
            Name: B64("Updated Name"),
            Answers: new List<AnswerInputRequest>(),
            AdvanceStep: true);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_mainService.Id.Value}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.Name.Should().Be("Updated Name");
        service.CurrentStep.Should().Be(2);
        service.Status.Should().Be(0); // Draft
    }

    [Fact]
    public async Task ShouldUpdateStepTwo_AdvancesToStepThree()
    {
        // Arrange
        var request = new UpdateServiceStepTwoRequest(
            Answers: new List<AnswerInputRequest>(),
            AdvanceStep: true);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_serviceAtStepTwo.Id.Value}/step-two", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.CurrentStep.Should().Be(3);
    }

    [Fact]
    public async Task ShouldUpdatePublishedService_RevertsToDraft()
    {
        // Arrange
        var request = new UpdateServiceStepOneRequest(
            Name: B64("Updated Published Service"),
            Answers: new List<AnswerInputRequest>(),
            AdvanceStep: false);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{_completedService.Id.Value}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.Status.Should().Be(0); // Draft after edit
    }

    [Fact]
    public async Task ShouldCompleteService_SetsStatusToComplete()
    {
        // Arrange - use a service that's ready to be completed (at step 3)
        // For this test, we'll use the completed service which was already at step 3 before completion
        // But since it's already complete, we need to create a new service and advance it
        var createResponse = await Client.PostAsJsonAsync("services/create", new CreateServiceRequest(B64("Service To Complete")));
        var createdService = await createResponse.ToResponseModel<ServiceDto>();

        // Advance to step 2
        await Client.PutAsJsonAsync(
            $"services/{createdService.Id}/step-one",
            new UpdateServiceStepOneRequest(B64("Service To Complete"), new List<AnswerInputRequest>(), true));

        // Advance to step 3
        await Client.PutAsJsonAsync(
            $"services/{createdService.Id}/step-two",
            new UpdateServiceStepTwoRequest(new List<AnswerInputRequest>(), true));

        // Act
        var response = await Client.PostAsync($"services/{createdService.Id}/complete", null);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.Status.Should().Be(1); // Complete
    }

    [Fact]
    public async Task ShouldUpdateStepOne_WithClosingDate_SkipsStepTwoAndAdvancesToStepThree()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("services/create", new CreateServiceRequest(B64("No Longer Offered Service")));
        var createdService = await createResponse.ToResponseModel<ServiceDto>();

        // SMD04 (closing date) being present signals the service is no longer offered — step 2 should be skipped
        var request = new UpdateServiceStepOneRequest(
            Name: B64("No Longer Offered Service"),
            Answers: [new("SMD04", B64("2024-01-01"))],
            AdvanceStep: true);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{createdService.Id}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.CurrentStep.Should().Be(3);
    }

    [Fact]
    public async Task ShouldUpdateStepOne_WithClosingDate_ClearsExistingStepTwoAnswers()
    {
        // Arrange - start with a service at step 2 that has step 2 answers
        var createResponse = await Client.PostAsJsonAsync("services/create", new CreateServiceRequest(B64("Service With Step Two Answers")));
        var createdService = await createResponse.ToResponseModel<ServiceDto>();

        // Advance to step 2 without a closing date
        await Client.PutAsJsonAsync(
            $"services/{createdService.Id}/step-one",
            new UpdateServiceStepOneRequest(
                Name: B64("Service With Step Two Answers"),
                Answers: [],
                AdvanceStep: true));

        // Add some step 2 answers
        await Client.PutAsJsonAsync(
            $"services/{createdService.Id}/step-two",
            new UpdateServiceStepTwoRequest(
                Answers: [new("SMD06", B64("weekly"))],
                AdvanceStep: false));

        // Now update step 1 with a closing date — step 2 answers should be cleared
        var request = new UpdateServiceStepOneRequest(
            Name: B64("Service With Step Two Answers"),
            Answers: [new("SMD04", B64("2024-01-01"))],
            AdvanceStep: true);

        // Act
        var response = await Client.PutAsJsonAsync($"services/{createdService.Id}/step-one", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var service = await response.ToResponseModel<ServiceDto>();
        service.CurrentStep.Should().Be(3);
        service.Answers.Should().NotContain(a => a.QuestionCode == "SMD06");
    }

    [Fact]
    public async Task ShouldDeleteService_ReturnsNoContent()
    {
        // Arrange - create a service to delete
        var createResponse = await Client.PostAsJsonAsync("services/create", new CreateServiceRequest(B64("Service To Delete")));
        var createdService = await createResponse.ToResponseModel<ServiceDto>();

        // Act
        var response = await Client.DeleteAsync($"services/{createdService.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Context.Services.Any(x => x.Id == new ServiceId(createdService.Id!.Value)).Should().BeFalse();
    }

    public async Task InitializeAsync()
    {
        // Create organisation with ID from OrganisationAdminUsersData
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        // Create test services
        _mainService = ServicesData.MainService(OrganisationAdminUsersData.OrganisationId);
        _serviceAtStepTwo = ServicesData.ServiceAtStepTwo(OrganisationAdminUsersData.OrganisationId);
        _completedService = ServicesData.CompletedService(OrganisationAdminUsersData.OrganisationId);

        Context.Services.Add(_mainService);
        Context.Services.Add(_serviceAtStepTwo);
        Context.Services.Add(_completedService);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}