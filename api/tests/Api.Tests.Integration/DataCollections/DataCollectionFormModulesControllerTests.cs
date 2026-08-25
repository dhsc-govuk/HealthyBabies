using System.Net;
using System.Net.Http.Json;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using FluentAssertions;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

public class DataCollectionFormModulesControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string CreateRoute = "data-collection-form-questions/modules";

    [Fact]
    public async Task ShouldGenerateCodeFromNameAndNotAcceptAClientSuppliedCode()
    {
        // Arrange
        var request = new { name = "My New Module", description = "A test module", type = DataCollectionTypeConstants.DeliveryPlan, code = "should-be-ignored" };

        // Act
        var response = await Client.PostAsJsonAsync(CreateRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var module = await response.ToResponseModel<DataCollectionFormModuleDto>();
        module.Code.Should().Be("my-new-module");

        Context.DataCollectionFormModules
            .Any(m => m.Id == new DataCollectionFormModuleId(module.Id) && m.Code == "my-new-module")
            .Should().BeTrue();
    }

    [Fact]
    public async Task ShouldDeDuplicateGeneratedCodeWhenNameCollides()
    {
        // Arrange
        var first = new { name = "Duplicate Name Module", description = (string?)null, type = DataCollectionTypeConstants.DeliveryPlan };
        var second = new { name = "Duplicate Name Module", description = (string?)null, type = DataCollectionTypeConstants.DeliveryPlan };

        // Act
        var firstResponse = await Client.PostAsJsonAsync(CreateRoute, first);
        var secondResponse = await Client.PostAsJsonAsync(CreateRoute, second);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstModule = await firstResponse.ToResponseModel<DataCollectionFormModuleDto>();
        var secondModule = await secondResponse.ToResponseModel<DataCollectionFormModuleDto>();

        firstModule.Code.Should().Be("duplicate-name-module");
        secondModule.Code.Should().Be("duplicate-name-module-2");
    }

    [Fact]
    public async Task ShouldRejectCreateWhenNameIsMissing()
    {
        // Arrange
        var request = new { name = "", description = (string?)null, type = DataCollectionTypeConstants.DeliveryPlan };

        // Act
        var response = await Client.PostAsJsonAsync(CreateRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}