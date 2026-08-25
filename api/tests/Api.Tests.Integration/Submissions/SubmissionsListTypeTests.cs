using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// The Submissions list page needs to tell a Delivery Plan apart from a Management Information
/// data collection so the LA can filter the "Past" list by type.
/// </summary>
public class SubmissionsListTypeTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private DataCollection _deliveryPlan = null!;
    private DataCollection _managementInformation = null!;

    [Fact]
    public async Task GetAll_ReturnsCorrectTypeForEachDataCollection()
    {
        var response = await Client.GetAsync("organisation-admin/submissions");

        response.IsSuccessStatusCode.Should().BeTrue();
        var submissions = await response.ToResponseModel<List<SubmissionDto>>();

        submissions.Should().Contain(s => s.Id == _deliveryPlan.Id.Value && s.Type == DataCollectionTypeConstants.DeliveryPlan);
        submissions.Should().Contain(s => s.Id == _managementInformation.Id.Value && s.Type == DataCollectionTypeConstants.ManagementInformation);
    }

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        _deliveryPlan = DataCollection.New(
            id: DataCollectionId.New(),
            name: "2026 Delivery Plan",
            description: null,
            startDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            type: DataCollectionType.DeliveryPlan);

        _managementInformation = DataCollection.New(
            id: DataCollectionId.New(),
            name: "2026 Q1 Management Information",
            description: null,
            startDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
            type: DataCollectionType.ManagementInformation);

        Context.DataCollections.Add(_deliveryPlan);
        Context.DataCollections.Add(_managementInformation);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}