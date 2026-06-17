using System.Net;
using System.Net.Http.Json;
using Application.Organisations.Dtos;
using Application.SiteForms.Dtos;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Locations;

public class LocationsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _mainOrganisation = OrganisationsData.MainOrganisation;
    private readonly Location _mainLocation = LocationsData.MainLocation(OrganisationsData.MainOrganisation.Id);
    private readonly Location _secondLocation = LocationsData.SecondLocation(OrganisationsData.MainOrganisation.Id);
    private readonly IReadOnlyList<SiteFormQuestion> _predefinedQuestions = SiteFormQuestionsData.GetPredefinedQuestions();

    private string MainCreateLocationRoute => $"organisations/{_mainOrganisation.Id}/locations/create";
    private string MainUpdateLocationRoute => $"organisations/{_mainOrganisation.Id}/locations/edit";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    [Fact]
    public async Task ShouldCreateLocation()
    {
        // Arrange
        var request = new CreateLocationInputDto(
            Name: null,
            PostCode: null,
            ReferenceNumber: null,
            AddressLine1: null,
            AddressLine2: null,
            TownOrCity: null,
            County: null,
            Answers: new List<SiteAnswerInputDto>
            {
                new("FHS01", B64(_mainLocation.Name)),
                new("FHS02", B64(_mainLocation.PostCode ?? "AB12 3CD")),
                new("FHS03", B64(_mainLocation.ReferenceNumber))
            });

        // Act
        var response = await Client.PostAsJsonAsync(MainCreateLocationRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var location = await response.ToResponseModel<LocationDto>();
        Context.Locations.Any(x => x.Id == new LocationId(location.Id!.Value)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateLocationBecauseNameDuplication()
    {
        // Arrange
        var request = new CreateLocationInputDto(
            Name: null,
            PostCode: null,
            ReferenceNumber: null,
            AddressLine1: null,
            AddressLine2: null,
            TownOrCity: null,
            County: null,
            Answers: new List<SiteAnswerInputDto>
            {
                new("FHS01", B64(_secondLocation.Name)),
                new("FHS02", B64(_secondLocation.PostCode ?? "AB12 3CD")),
                new("FHS03", B64(Guid.NewGuid().ToString()))
            });

        // Act
        var response = await Client.PostAsJsonAsync(MainCreateLocationRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldNotCreateLocationBecauseReferenceNumberDuplication()
    {
        // Arrange
        var request = new CreateLocationInputDto(
            Name: null,
            PostCode: null,
            ReferenceNumber: null,
            AddressLine1: null,
            AddressLine2: null,
            TownOrCity: null,
            County: null,
            Answers: new List<SiteAnswerInputDto>
            {
                new("FHS01", B64("Unique Location Name")),
                new("FHS02", B64("AB12 3CD")),
                new("FHS03", B64(_secondLocation.ReferenceNumber))
            });

        // Act
        var response = await Client.PostAsJsonAsync(MainCreateLocationRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldUpdateLocation()
    {
        // Arrange
        const string newLocationName = "New location name";
        var request = new UpdateLocationInputDto(
            Id: _secondLocation.Id.Value,
            Name: null,
            PostCode: null,
            ReferenceNumber: null,
            AddressLine1: null,
            AddressLine2: null,
            TownOrCity: null,
            County: null,
            IsActive: _secondLocation.IsActive,
            Answers: new List<SiteAnswerInputDto>
            {
                new("FHS01", B64(newLocationName)),
                new("FHS02", B64(_secondLocation.PostCode ?? "AB12 3CD")),
                new("FHS03", B64(_secondLocation.ReferenceNumber))
            });

        // Act
        var response = await Client.PutAsJsonAsync(MainUpdateLocationRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var location = await response.ToResponseModel<LocationDto>();
        Context.Locations.First(x => x.Id == new LocationId(location.Id!.Value))
            .Name.Should().Be(newLocationName);
    }

    [Fact]
    public async Task ShouldNotUpdateLocationBecauseReferenceNumberDuplication()
    {
        // Arrange - create a third location first
        var thirdLocation = LocationsData.MainLocation(_mainOrganisation.Id);
        Context.Locations.Add(thirdLocation);
        await SaveChangesAsync();

        var request = new UpdateLocationInputDto(
            Id: thirdLocation.Id.Value,
            Name: null,
            PostCode: null,
            ReferenceNumber: null,
            AddressLine1: null,
            AddressLine2: null,
            TownOrCity: null,
            County: null,
            IsActive: true,
            Answers: new List<SiteAnswerInputDto>
            {
                new("FHS01", B64("Different Name")),
                new("FHS02", B64("AB12 3CD")),
                new("FHS03", B64(_secondLocation.ReferenceNumber))
            });

        // Act
        var response = await Client.PutAsJsonAsync(MainUpdateLocationRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    public async Task InitializeAsync()
    {
        Context.Organisations.Add(_mainOrganisation);

        if (!Context.SiteFormQuestions.Any())
        {
            Context.SiteFormQuestions.AddRange(_predefinedQuestions);
        }

        Context.Locations.Add(_secondLocation);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}