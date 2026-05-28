using System.Net.Http.Json;
using Application.Users.Dtos;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

public class OrganisationUsersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Organisation _mainOrganisation = OrganisationsData.MainOrganisation;

    private readonly OrganisationUser _mainOrganisationAdmin;
    private readonly OrganisationUser _secondOrganisationAdmin;

    private readonly string _mainCreateOrganisationUserRoute;
    private readonly string _mainUpdateOrganisationUserRoute;

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    public OrganisationUsersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _mainOrganisationAdmin = OrganisationUsersData.MainOrganisationUser(_mainOrganisation.Id);
        _secondOrganisationAdmin = OrganisationUsersData.SecondOrganisationUser(_mainOrganisation.Id);

        _mainCreateOrganisationUserRoute = $"organisations/{_mainOrganisation.Id}/users/create";
        _mainUpdateOrganisationUserRoute = $"organisations/{_mainOrganisation.Id}/users/edit";
    }

    [Fact]
    public async Task ShouldCreateOrganisationAdmin()
    {
        // Arrange
        var firstName = _mainOrganisationAdmin.User!.Name.FirstName;
        var lastName = _mainOrganisationAdmin.User.Name.LastName;
        var request = new UserDto(
            Id: null,
            FirstName: B64(firstName),
            LastName: B64(lastName),
            Email: _mainOrganisationAdmin.User.Email,
            IsActive: _mainOrganisationAdmin.User.IsActive,
            Role: _mainOrganisationAdmin.User.Role.ToString());

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateOrganisationUserRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var organisationAdmin = await response.ToResponseModel<UserDto>();
        var dbOrganisationAdmin = await Context.OrganisationUsers
            .AsNoTracking()
            .Include(x => x.User)
            .FirstAsync(x => x.Id == new OrganisationUserId(organisationAdmin.Id!.Value));
        dbOrganisationAdmin.User!.Name.FirstName.Should().Be(firstName);
        dbOrganisationAdmin.User.Name.LastName.Should().Be(lastName);
        dbOrganisationAdmin.User.Email.Should().Be(request.Email);
        dbOrganisationAdmin.User.IsActive.Should().Be(request.IsActive);
        dbOrganisationAdmin.User.Role.Should().Be(UserRole.OrganisationAdmin);
    }

    [Fact]
    public async Task ShouldUpdateOrganisationAdmin()
    {
        // Arrange
        var request = new UserDto(
            Id: _secondOrganisationAdmin.Id.Value,
            FirstName: _mainOrganisationAdmin.User!.Name.FirstName,
            LastName: _mainOrganisationAdmin.User.Name.LastName,
            Email: _mainOrganisationAdmin.User.Email,
            IsActive: _mainOrganisationAdmin.User.IsActive,
            Role: _mainOrganisationAdmin.User.Role.ToString());

        // Act
        var response = await Client.PutAsJsonAsync(_mainUpdateOrganisationUserRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var organisationAdmin = await response.ToResponseModel<UserDto>();
        var dbOrganisationAdmin = await Context.OrganisationUsers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id.Equals(new OrganisationUserId(organisationAdmin.Id!.Value)));
        dbOrganisationAdmin!.User!.Name.FirstName.Should().Be(request.FirstName);
        dbOrganisationAdmin.User.Name.LastName.Should().Be(request.LastName);
        dbOrganisationAdmin.User.Email.Should().Be(request.Email);
        dbOrganisationAdmin.User.IsActive.Should().Be(request.IsActive);
        dbOrganisationAdmin.User.Role.Should().Be(UserRole.OrganisationAdmin);
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();

        await Context.Organisations.AddAsync(_mainOrganisation);
        await Context.OrganisationUsers.AddAsync(_secondOrganisationAdmin);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}