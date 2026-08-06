using System.Net.Http.Json;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class CheckServiceUserDuplicatesTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private const string ServiceWithData = "Service With Existing Data";
    private const string ServiceWithoutData = "Service Without Existing Data";
    private const string UnknownService = "Service That Does Not Exist";
    private static readonly Guid DataCollectionGuid = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    private Guid _moduleId;

    [Fact]
    public async Task CheckDuplicates_ReturnsHasExistingData_WhenServiceAlreadyHasSubmission()
    {
        var response = await PostCheckAsync(new[]
        {
            new ServiceUserRowCheckDto(0, ServiceWithData),
        });

        response.Should().HaveCount(1);
        response[0].RowIndex.Should().Be(0);
        response[0].ServiceName.Should().Be(ServiceWithData);
        response[0].HasExistingData.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDuplicates_ReturnsNoExistingData_WhenServiceHasNoSubmission()
    {
        var response = await PostCheckAsync(new[]
        {
            new ServiceUserRowCheckDto(0, ServiceWithoutData),
        });

        response.Should().HaveCount(1);
        response[0].HasExistingData.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDuplicates_ReturnsNoExistingData_WhenServiceNameIsUnknown()
    {
        var response = await PostCheckAsync(new[]
        {
            new ServiceUserRowCheckDto(0, UnknownService),
        });

        response.Should().HaveCount(1);
        response[0].HasExistingData.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDuplicates_ReturnsCorrectStatusPerRow_ForMixedBatch()
    {
        var response = await PostCheckAsync(new[]
        {
            new ServiceUserRowCheckDto(0, ServiceWithData),
            new ServiceUserRowCheckDto(1, ServiceWithoutData),
            new ServiceUserRowCheckDto(2, UnknownService),
        });

        response.Should().HaveCount(3);
        response.Should().ContainSingle(r => r.RowIndex == 0 && r.HasExistingData == true);
        response.Should().ContainSingle(r => r.RowIndex == 1 && r.HasExistingData == false);
        response.Should().ContainSingle(r => r.RowIndex == 2 && r.HasExistingData == false);
    }

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        var serviceWithData = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceWithData);
        Context.Services.Add(serviceWithData);

        Context.Services.Add(Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceWithoutData));

        await SaveChangesAsync();

        var serviceUsersModule = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
        _moduleId = serviceUsersModule.Id.Value;

        // Seed an existing FormSubmission for ServiceWithData so it reports hasExistingData = true.
        var submission = FormSubmission.Create(
            formModuleId: serviceUsersModule.Id,
            organisationId: OrganisationAdminUsersData.OrganisationId,
            dataCollectionId: new DataCollectionId(DataCollectionGuid),
            entityType: "Service",
            entityId: serviceWithData.Id.Value);
        Context.FormSubmissions.Add(submission);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private async Task<List<ServiceUserDuplicateCheckDto>> PostCheckAsync(IEnumerable<ServiceUserRowCheckDto> rows)
    {
        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{_moduleId}/bulk-upload/check-service-user-duplicates";
        var response = await Client.PostAsJsonAsync(url, new CheckServiceUserDuplicatesRequestDto(rows.ToList()));

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<List<ServiceUserDuplicateCheckDto>>();
    }
}