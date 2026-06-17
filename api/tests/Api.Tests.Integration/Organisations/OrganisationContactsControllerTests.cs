using System.Net;
using System.Net.Http.Json;
using Application.Organisations.Dtos;
using Domain.Organisations;
using Domain.Systems;
using Domain.Users;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

public class OrganisationContactsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;
    private readonly OrganisationContact _secondContact;
    private readonly GlobalData _mainContactRole;
    private readonly User _mainAdmin;

    public OrganisationContactsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _mainAdmin = AdminsData.MainAdmin;
        _secondContact = OrganisationContactsData.SecondContact(_organisation.Id);
        _mainContactRole = GlobalDataData.CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Director", "Existing description");
    }

    private string GetBaseRoute() => $"organisations/{_organisation.Id.Value}/contacts";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    [Fact]
    public async Task ShouldCreateOrganisationContact()
    {
        // Arrange
        var request = new CreateOrganisationContactDto(
            OrganisationId: _organisation.Id.Value,
            Name: B64("New Contact"),
            Email: "new@example.com",
            Role: B64("Director"));

        // Act
        var response = await Client.PostAsJsonAsync(GetBaseRoute(), request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var contact = await response.ToResponseModel<OrganisationContactDto>();
        contact.Name.Should().Be("New Contact");
        contact.Email.Should().Be("new@example.com");
        contact.Role.Should().Be("Director");
        Context.OrganisationContacts.Any(x => x.Id == new OrganisationContactId(contact.Id!.Value)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldGetAllOrganisationContacts()
    {
        // Act
        var response = await Client.GetAsync(GetBaseRoute());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var contacts = await response.ToResponseModel<List<OrganisationContactDto>>();
        contacts.Should().NotBeEmpty();
        contacts.Should().Contain(x => x.Id == _secondContact.Id.Value);
    }

    [Fact]
    public async Task ShouldGetOrganisationContactById()
    {
        // Act
        var response = await Client.GetAsync($"{GetBaseRoute()}/{_secondContact.Id.Value}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var contact = await response.ToResponseModel<OrganisationContactDto>();
        contact.Id.Should().Be(_secondContact.Id.Value);
        contact.Name.Should().Be(_secondContact.Name);
    }

    [Fact]
    public async Task ShouldUpdateOrganisationContact()
    {
        // Arrange
        var request = new UpdateOrganisationContactDto(
            Id: _secondContact.Id.Value,
            Name: "Updated Name",
            Email: "updated@example.com",
            Role: "Director");

        // Act
        var response = await Client.PutAsJsonAsync($"{GetBaseRoute()}/{_secondContact.Id.Value}", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var contact = await response.ToResponseModel<OrganisationContactDto>();
        contact.Name.Should().Be("Updated Name");
        contact.Email.Should().Be("updated@example.com");
        contact.Role.Should().Be("Director");
    }

    [Fact]
    public async Task ShouldDeleteOrganisationContact()
    {
        // Arrange
        var contactToDelete = _secondContact;

        // Act
        var response = await Client.DeleteAsync($"{GetBaseRoute()}/{contactToDelete.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var deletedContact = Context.OrganisationContacts.FirstOrDefault(x => x.Id == contactToDelete.Id);
        deletedContact.Should().BeNull(); // Should be filtered by query filter
    }

    [Fact]
    public async Task ShouldReturnNotFoundForNonExistentContact()
    {
        // Act
        var response = await Client.GetAsync($"{GetBaseRoute()}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        Context.Users.Add(_mainAdmin);
        Context.GlobalData.Add(_mainContactRole);
        Context.Organisations.Add(_organisation);
        Context.OrganisationContacts.Add(_secondContact);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}