using Domain.Locations;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.CoreData;

public class OrganisationAdminCoreDataControllerTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _otherOrganisation = OrganisationsData.SecondOrganisation;

    private Location _otherLocation = null!;
    private Service _otherService = null!;

    [Fact]
    public async Task DownloadSites_ReturnsCsv_ContainingOnlyCallersOwnLocations()
    {
        // Act
        var response = await Client.GetAsync("organisation-admin/core-data/sites/download");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("Main");
        csv.Should().Contain("Second Location");
        csv.Should().NotContain(_otherLocation.ReferenceNumber);
    }

    [Fact]
    public async Task DownloadSites_CsvDoesNotIncludeLANameColumn()
    {
        // Act
        var response = await Client.GetAsync("organisation-admin/core-data/sites/download");

        // Assert
        var headerRow = await FirstLine(response);
        headerRow.Should().NotContain("LAName");
        headerRow.Should().Contain("SiteName");
    }

    [Fact]
    public async Task DownloadServices_ReturnsCsv_ContainingOnlyCallersOwnServices()
    {
        // Act
        var response = await Client.GetAsync("organisation-admin/core-data/services/download");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("Main Service");
        csv.Should().NotContain(_otherService.Id.Value.ToString());
    }

    [Fact]
    public async Task DownloadServices_CsvDoesNotIncludeLANameColumn()
    {
        // Act
        var response = await Client.GetAsync("organisation-admin/core-data/services/download");

        // Assert
        var headerRow = await FirstLine(response);
        headerRow.Should().NotContain("LAName");
        headerRow.Should().Contain("ServiceName");
    }

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);
        Context.Organisations.Add(_otherOrganisation);

        _otherLocation = LocationsData.MainLocation(_otherOrganisation.Id);
        _otherService = ServicesData.MainService(_otherOrganisation.Id);

        Context.Locations.Add(LocationsData.MainLocation(OrganisationAdminUsersData.OrganisationId));
        Context.Locations.Add(LocationsData.SecondLocation(OrganisationAdminUsersData.OrganisationId));
        Context.Locations.Add(_otherLocation);

        Context.Services.Add(ServicesData.MainService(OrganisationAdminUsersData.OrganisationId));
        Context.Services.Add(_otherService);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static async Task<string> FirstLine(HttpResponseMessage response)
    {
        var csv = await response.Content.ReadAsStringAsync();
        return csv.Split('\n').First();
    }
}