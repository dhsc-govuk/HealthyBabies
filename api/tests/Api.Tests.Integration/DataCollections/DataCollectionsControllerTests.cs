using System.Net;
using System.Net.Http.Json;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

public class DataCollectionsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;

    private readonly DataCollection _mainDataCollection = DataCollectionsData.MainDataCollection;
    private readonly DataCollection _secondDataCollection = DataCollectionsData.SecondDataCollection;

    private readonly Organisation _mainOrganisation = OrganisationsData.MainOrganisation;
    private readonly Organisation _secondOrganisation = OrganisationsData.SecondOrganisation;

    private readonly string _mainCreateRoute = "admin/data-collections/create";
    private readonly string _mainUpdateRoute = "admin/data-collections/edit";
    private readonly string _mainGetAllRoute = "admin/data-collections";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    private static string? B64N(string? value) => value is not null ? "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)) : null;

    [Fact]
    public async Task ShouldCreateDataCollection()
    {
        // Arrange
        var request = new CreateDataCollectionDto(
            Name: B64(_mainDataCollection.Name),
            Description: B64N(_mainDataCollection.Description),
            StartDate: _mainDataCollection.StartDate,
            EndDate: _mainDataCollection.EndDate);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        Context.DataCollections.Any(x => x.Id == new DataCollectionId(dataCollection.Id!.Value)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateDataCollectionBecauseNameDuplication()
    {
        // Arrange
        var request = new CreateDataCollectionDto(
            Name: B64(_secondDataCollection.Name),
            Description: B64N(_secondDataCollection.Description),
            StartDate: _secondDataCollection.StartDate,
            EndDate: _secondDataCollection.EndDate);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldUpdateDataCollection()
    {
        // Arrange
        const string newName = "Updated Data Collection Name";
        var request = new UpdateDataCollectionDto(
            Name: B64(newName),
            Description: B64N(_secondDataCollection.Description),
            StartDate: _secondDataCollection.StartDate,
            EndDate: _secondDataCollection.EndDate);

        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/edit";

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        Context.DataCollections.First(x => x.Id == new DataCollectionId(dataCollection.Id!.Value))
            .Name.Should().Be(newName);
    }

    [Fact]
    public async Task ShouldGetAllDataCollections()
    {
        // Act
        var response = await Client.GetAsync(_mainGetAllRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollections = await response.ToResponseModel<List<DataCollectionDto>>();
        dataCollections.Should().NotBeNull();
        dataCollections.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ShouldGetDataCollectionById()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.Should().NotBeNull();
        dataCollection.Name.Should().Be(_secondDataCollection.Name);
    }

    [Fact]
    public async Task ShouldDeleteDataCollection()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var deletedDataCollection = Context.DataCollections
            .IgnoreQueryFilters()
            .First(x => x.Id == _secondDataCollection.Id);
        deletedDataCollection.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldGetDataCollectionWithLocalAuthorities()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.Should().NotBeNull();
        dataCollection.LocalAuthorities.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldUpdateLocalAuthorities()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities";
        var request = new UpdateDataCollectionLocalAuthoritiesDto(
            LocalAuthorityIds: new List<Guid> { _mainOrganisation.Id.Value, _secondOrganisation.Id.Value });

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.LocalAuthorities.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldRemoveLocalAuthorityFromDataCollection()
    {
        // Arrange
        _secondDataCollection.AssignLocalAuthority(_mainOrganisation.Id);
        await SaveChangesAsync();

        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities/{_mainOrganisation.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.LocalAuthorities.Should().BeEmpty();
    }

    public async Task InitializeAsync()
    {
        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_mainOrganisation);
        Context.Organisations.Add(_secondOrganisation);
        Context.DataCollections.Add(_secondDataCollection);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}