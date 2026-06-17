using System.Net;
using Application.Common.Dtos;
using FluentAssertions;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.AddressLookup;

public class AddressLookupControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    [Fact]
    public async Task ShouldReturnAddressesForValidPostcode()
    {
        // Act
        var response = await Client.GetAsync("address-lookup?postcode=SW1A1AA");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var addresses = await response.ToResponseModel<List<OsPlacesAddressDto>>();
        addresses.Should().NotBeEmpty();
        addresses.Should().HaveCount(2);
        addresses[0].Uprn.Should().NotBeNullOrEmpty();
        addresses[0].PostTown.Should().Be("LONDON");
    }

    [Fact]
    public async Task ShouldReturnBadRequestForEmptyPostcode()
    {
        // Act
        var response = await Client.GetAsync("address-lookup?postcode=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnBadRequestForMissingPostcode()
    {
        // Act
        var response = await Client.GetAsync("address-lookup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}