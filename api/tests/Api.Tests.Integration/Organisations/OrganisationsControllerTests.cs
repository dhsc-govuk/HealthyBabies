using System.Net;
using System.Net.Http.Json;
using Application.Organisations.Dtos;
using Domain.Organisations;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

public class OrganisationsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _mainOrganisation = OrganisationsData.MainOrganisation;
    private readonly Organisation _secondOrganisation = OrganisationsData.SecondOrganisation;

    private readonly string _mainCreateOrganisationRoute = "organisations/create";
    private readonly string _mainUpdateOrganisationRoute = "organisations/edit";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    private static string? B64N(string? value) => value is not null ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)) : null;

    [Fact]
    public async Task ShouldCreateOrganisation()
    {
        // Arrange
        var request = new CreateOrganisationDto(
            Id: null,
            Name: B64(_mainOrganisation.Name),
            ONSCode: _mainOrganisation.ONSCode,
            IsActive: _mainOrganisation.IsActive);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateOrganisationRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var organisation = await response.ToResponseModel<OrganisationDto>();
        Context.Organisations.Any(x => x.Id == new OrganisationId(organisation.Id!.Value)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldCreateOrganisationWithContacts()
    {
        // Arrange
        var contacts = new List<CreateContactDto>
        {
            new(B64("John Doe"), B64("Admin"), null, "john.doe@example.com"),
            new(B64("Jane Smith"), B64("Manager"), null, "jane.smith@example.com")
        };
        var request = new CreateOrganisationDto(
            Id: null,
            Name: B64("Organisation With Contacts"),
            ONSCode: "E09000099",
            IsActive: true,
            Contacts: contacts);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateOrganisationRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var organisation = await response.ToResponseModel<OrganisationDto>();
        var organisationId = new OrganisationId(organisation.Id!.Value);
        Context.Organisations.Any(x => x.Id == organisationId).Should().BeTrue();
        Context.OrganisationContacts.Count(x => x.OrganisationId == organisationId).Should().Be(2);
    }

    [Fact]
    public async Task ShouldNotCreateOrganisationBecauseNameDuplication()
    {
        // Arrange
        var request = new OrganisationDto(
            Id: null,
            Name: B64(_secondOrganisation.Name),
            ONSCode: _secondOrganisation.ONSCode,
            IsActive: _secondOrganisation.IsActive);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateOrganisationRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldUpdateOrganisation()
    {
        // Arrange
        const string newOrganisationName = "New organisation name";
        var request = new OrganisationDto(
            Id: _secondOrganisation.Id.Value,
            Name: newOrganisationName,
            ONSCode: _secondOrganisation.ONSCode,
            IsActive: _secondOrganisation.IsActive);

        // Act
        var response = await Client.PutAsJsonAsync(_mainUpdateOrganisationRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var organisation = await response.ToResponseModel<OrganisationDto>();
        Context.Organisations.First(x => x.Id == new OrganisationId(organisation.Id!.Value))
            .Name.Should().Be(newOrganisationName);
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        Context.Organisations.Add(_secondOrganisation);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}