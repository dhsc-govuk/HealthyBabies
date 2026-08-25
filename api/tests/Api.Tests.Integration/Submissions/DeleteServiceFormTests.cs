using System.Net;
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

// BLUM-7184. The delete link on a service's quarterly record had been shipped without its server
// half: the route existed for GET and POST only, so DELETE matched the path, found no action for the
// verb and came back 405 Method Not Allowed - the click appeared to do nothing.
public class DeleteServiceFormTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private static readonly DateTime QuarterStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _module = null!;
    private Service _service = null!;
    private Service _otherOrganisationService = null!;
    private static readonly OrganisationId OtherOrganisationId = OrganisationId.New();

    [Fact]
    public async Task DeleteServiceForm_RemovesTheRecord_ForAnOpenQuarter()
    {
        var submissionId = await SeedServiceFormAsync(_service, OrganisationAdminUsersData.OrganisationId);

        var response = await Client.DeleteAsync(Url(_service.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Context.FormSubmissions.AnyAsync(s => s.Id == submissionId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteServiceForm_IsRouted_AndDoesNotReturnMethodNotAllowed()
    {
        var response = await Client.DeleteAsync(Url(Guid.NewGuid()));

        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Record not found");
    }

    [Fact]
    public async Task DeleteServiceForm_LeavesTheRecordAlone_WhenItBelongsToAnotherOrganisation()
    {
        var submissionId = await SeedServiceFormAsync(_otherOrganisationService, OtherOrganisationId);

        var response = await Client.DeleteAsync(Url(_otherOrganisationService.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Context.FormSubmissions.AnyAsync(s => s.Id == submissionId)).Should().BeTrue();
    }

    // organisation_id was added to form_submissions nullable and never backfilled (20260309161714),
    // so records predating it carry no organisation. Ownership comes off the Service instead, or the
    // organisation that owns the record could not delete it while still being able to edit it.
    [Fact]
    public async Task DeleteServiceForm_RemovesTheRecord_WhenItPredatesTheOrganisationColumn()
    {
        var submission = FormSubmission.Create(
            formModuleId: _module.Id,
            organisationId: null,
            dataCollectionId: _dataCollection.Id,
            entityType: "Service",
            entityId: _service.Id.Value);
        Context.FormSubmissions.Add(submission);
        await SaveChangesAsync();

        var response = await Client.DeleteAsync(Url(_service.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Context.FormSubmissions.AnyAsync(s => s.Id == submission.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteServiceForm_RefusesAndKeepsTheRecord_WhenTheQuarterHasBeenSubmitted()
    {
        var submissionId = await SeedServiceFormAsync(_service, OrganisationAdminUsersData.OrganisationId);

        var quarterSubmission = DataCollectionSubmission.Create(_dataCollection.Id, OrganisationAdminUsersData.OrganisationId);
        quarterSubmission.SetStatus(DataCollectionSubmissionStatus.Submitted);
        Context.DataCollectionSubmissions.Add(quarterSubmission);
        await SaveChangesAsync();

        var response = await Client.DeleteAsync(Url(_service.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("can no longer be edited");
        (await Context.FormSubmissions.AnyAsync(s => s.Id == submissionId)).Should().BeTrue();
    }

    private string Url(Guid serviceId) =>
        $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_module.Id.Value}/services/{serviceId}";

    private async Task<FormSubmissionId> SeedServiceFormAsync(Service service, OrganisationId organisationId)
    {
        var submission = FormSubmission.Create(
            formModuleId: _module.Id,
            organisationId: organisationId,
            dataCollectionId: _dataCollection.Id,
            entityType: "Service",
            entityId: service.Id.Value);

        Context.FormSubmissions.Add(submission);
        await SaveChangesAsync();

        return submission.Id;
    }

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        Context.Organisations.Add(Organisation.New(
            id: OtherOrganisationId,
            name: "Another Local Authority",
            oNSCode: "E09000002",
            isActive: true));

        _service = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Test Service - Family Hub Support");
        Context.Services.Add(_service);

        _otherOrganisationService = Service.New(
            id: ServiceId.New(),
            organisationId: OtherOrganisationId,
            name: "Another Authority's Service");
        Context.Services.Add(_otherOrganisationService);

        // End date in the future, so the quarter is open and only an explicit submission locks it.
        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Q1 2026",
            description: null,
            startDate: QuarterStart,
            endDate: DateTime.UtcNow.AddMonths(3));
        Context.DataCollections.Add(_dataCollection);

        await SaveChangesAsync();

        _module = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}