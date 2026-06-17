using System.Net;
using System.Net.Http.Json;
using Application.Systems.Dtos;
using Domain.Systems;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Systems;

public class GlobalDataControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string BaseRoute = "global-data";
    private readonly GlobalData _existingGlobalData = GlobalDataData.CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Existing Role", "Existing description");

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    private static string? B64N(string? value) => value is not null ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)) : null;

    [Fact]
    public async Task ShouldCreateGlobalData()
    {
        // Arrange
        var request = new CreateGlobalDataDto(
            Entity: B64(GlobalDataEntityTypes.ContactRole),
            Value: B64("New Role"),
            Description: B64("A new contact role"));

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var globalData = await response.ToResponseModel<GlobalDataDto>();
        globalData.Entity.Should().Be(GlobalDataEntityTypes.ContactRole);
        globalData.Value.Should().Be("New Role");
        globalData.Description.Should().Be("A new contact role");
        Context.GlobalData.Any(x => x.Id == new GlobalDataId(globalData.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnBadRequestForInvalidEntity()
    {
        // Arrange
        var request = new CreateGlobalDataDto(
            Entity: B64("InvalidEntity"),
            Value: B64("Some Value"),
            Description: null);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("InvalidEntity");
        content.Should().Contain("not valid");
    }

    [Fact]
    public async Task ShouldReturnConflictForDuplicateValue()
    {
        // Arrange
        var request = new CreateGlobalDataDto(
            Entity: B64(_existingGlobalData.Entity),
            Value: B64(_existingGlobalData.Value),
            Description: B64("Duplicate"));

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task ShouldGetAllGlobalData()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var globalDataList = await response.ToResponseModel<List<GlobalDataDto>>();
        globalDataList.Should().NotBeEmpty();
        globalDataList.Should().Contain(x => x.Id == _existingGlobalData.Id.Value);
    }

    [Fact]
    public async Task ShouldGetGlobalDataByEntity()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/entity/{_existingGlobalData.Entity}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var globalDataList = await response.ToResponseModel<List<GlobalDataDto>>();
        globalDataList.Should().NotBeEmpty();
        globalDataList.Should().OnlyContain(x => x.Entity == _existingGlobalData.Entity);
    }

    [Fact]
    public async Task ShouldGetGlobalDataById()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_existingGlobalData.Id.Value}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var globalData = await response.ToResponseModel<GlobalDataDto>();
        globalData.Id.Should().Be(_existingGlobalData.Id.Value);
        globalData.Entity.Should().Be(_existingGlobalData.Entity);
        globalData.Value.Should().Be(_existingGlobalData.Value);
    }

    [Fact]
    public async Task ShouldUpdateGlobalData()
    {
        // Arrange
        var request = new UpdateGlobalDataDto(
            Entity: _existingGlobalData.Entity,
            Value: "Updated Value",
            Description: "Updated description");

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{_existingGlobalData.Id.Value}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var globalData = await response.ToResponseModel<GlobalDataDto>();
        globalData.Value.Should().Be("Updated Value");
        globalData.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task ShouldReturnConflictWhenUpdatingToDuplicateValue()
    {
        // Arrange
        var anotherGlobalData = GlobalDataData.CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Another Role", "Another role");
        Context.GlobalData.Add(anotherGlobalData);
        await SaveChangesAsync();

        var request = new UpdateGlobalDataDto(
            Entity: _existingGlobalData.Entity,
            Value: anotherGlobalData.Value,
            Description: "Trying to duplicate");

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{_existingGlobalData.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task ShouldDeleteGlobalData()
    {
        // Arrange
        var globalDataToDelete = GlobalDataData.CreateGlobalData(GlobalDataEntityTypes.ContactRole, "To Delete", "Will be deleted");
        Context.GlobalData.Add(globalDataToDelete);
        await SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{globalDataToDelete.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var deletedData = Context.GlobalData.FirstOrDefault(x => x.Id == globalDataToDelete.Id);
        deletedData.Should().BeNull();
    }

    [Fact]
    public async Task ShouldReturnNotFoundForNonExistentGlobalData()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldGetEntityTypes()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/entity-types");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var entityTypes = await response.ToResponseModel<List<GlobalDataEntityTypeDto>>();
        entityTypes.Should().NotBeEmpty();
        entityTypes.Should().Contain(x => x.Name == GlobalDataEntityTypes.ContactRole);
    }

    public async Task InitializeAsync()
    {
        Context.GlobalData.Add(_existingGlobalData);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}