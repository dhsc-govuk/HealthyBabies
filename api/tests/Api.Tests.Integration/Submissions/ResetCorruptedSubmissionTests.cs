using System.Net;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// Tests the admin support tooling for BLUM-7047: detecting and repairing a data collection
/// submission left orphaned by a SubmitSubmission call that was cut off before the parent
/// DataCollectionSubmission record was written (the exact shape confirmed in Sandwell's
/// production data - "submitted" FormSubmissions with no parent row at all).
/// </summary>
public class ResetCorruptedSubmissionTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private static readonly DateTime QuarterStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime QuarterEnd = new(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _formModule = null!;

    [Fact]
    public async Task GetSubmissionIntegrity_ReportsCorrupted_WhenSubmittedChildrenHaveNoParentRow()
    {
        await SeedOrphanedSubmittedSubmissions(count: 2);

        var response = await Client.GetAsync(IntegrityUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var integrity = await response.ToResponseModel<SubmissionIntegrityDto>();
        integrity.IsCorrupted.Should().BeTrue();
        integrity.OrphanedSubmittedRecordCount.Should().Be(2);
        integrity.OrphanedFormSubmissionIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubmissionIntegrity_ReportsNotCorrupted_WhenNothingHasBeenSubmitted()
    {
        await SeedApprovedButNotSubmittedSubmissions(count: 2);

        var response = await Client.GetAsync(IntegrityUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var integrity = await response.ToResponseModel<SubmissionIntegrityDto>();
        integrity.IsCorrupted.Should().BeFalse();
        integrity.OrphanedSubmittedRecordCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSubmissionIntegrity_ReportsNotCorrupted_ForALegitimatelyReopenedSubmission()
    {
        // Reopen() only flips the parent DataCollectionSubmission to InProgress - it never touches
        // the children, so "submitted" children with a parent that isn't Submitted/Approved is the
        // NORMAL state right after a legitimate reopen, not corruption. The detection must key off
        // the parent's existence, not its status, or every reopened-but-not-yet-resubmitted quarter
        // would be misreported as corrupted.
        await SeedOrphanedSubmittedSubmissions(count: 2, withReopenedParent: true);

        var response = await Client.GetAsync(IntegrityUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var integrity = await response.ToResponseModel<SubmissionIntegrityDto>();
        integrity.IsCorrupted.Should().BeFalse();
        integrity.OrphanedSubmittedRecordCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetCorruptedSubmission_RevertsOrphanedRecords_AndIsIdempotentOnSecondCall()
    {
        var formSubmissionIds = await SeedOrphanedSubmittedSubmissions(count: 2);

        var firstResponse = await Client.PostAsync(ResetUrl(), content: null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, await firstResponse.Content.ReadAsStringAsync());
        var firstResult = await firstResponse.ToResponseModel<ResetCorruptedSubmissionResultDto>();
        firstResult.WasCorrupted.Should().BeTrue();
        firstResult.RecordsReset.Should().Be(2);

        var reverted = await Context.FormSubmissions
            .Where(s => formSubmissionIds.Contains(s.Id))
            .ToListAsync();
        reverted.Should().HaveCount(2);
        reverted.Should().OnlyContain(s => s.Status.Value == "approved" && s.SubmittedAt == null && s.SubmittedById == null);

        var secondResponse = await Client.PostAsync(ResetUrl(), content: null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK, await secondResponse.Content.ReadAsStringAsync());
        var secondResult = await secondResponse.ToResponseModel<ResetCorruptedSubmissionResultDto>();
        secondResult.WasCorrupted.Should().BeFalse();
        secondResult.RecordsReset.Should().Be(0);
    }

    private string IntegrityUrl() =>
        $"admin/organisations/{OrganisationAdminUsersData.OrganisationId.Value}/data-collections/{_dataCollection.Id.Value}/submission-integrity";

    private string ResetUrl() =>
        $"admin/organisations/{OrganisationAdminUsersData.OrganisationId.Value}/data-collections/{_dataCollection.Id.Value}/reset-corrupted-submission";

    private async Task<IReadOnlyList<FormSubmissionId>> SeedOrphanedSubmittedSubmissions(int count, bool withReopenedParent = false)
    {
        var ids = new List<FormSubmissionId>();

        for (var i = 0; i < count; i++)
        {
            var service = LiveService($"Live Service {i}");
            Context.Services.Add(service);

            var submission = FormSubmission.Create(
                _formModule.Id,
                OrganisationAdminUsersData.OrganisationId,
                _dataCollection.Id,
                entityType: "Service",
                entityId: service.Id.Value);
            submission.MarkAsComplete();
            submission.MarkAsSubmitted();
            Context.FormSubmissions.Add(submission);
            ids.Add(submission.Id);
        }

        if (withReopenedParent)
        {
            var parent = DataCollectionSubmission.Create(_dataCollection.Id, OrganisationAdminUsersData.OrganisationId);
            parent.Submit(OrganisationAdminUsersData.MainOrganisationAdminUser.Id);
            parent.Reopen();
            Context.DataCollectionSubmissions.Add(parent);
        }

        await SaveChangesAsync();
        return ids;
    }

    private async Task SeedApprovedButNotSubmittedSubmissions(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var service = LiveService($"Live Service {i}");
            Context.Services.Add(service);

            var submission = FormSubmission.Create(
                _formModule.Id,
                OrganisationAdminUsersData.OrganisationId,
                _dataCollection.Id,
                entityType: "Service",
                entityId: service.Id.Value);
            submission.MarkAsComplete();
            Context.FormSubmissions.Add(submission);
        }

        await SaveChangesAsync();
    }

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        Context.Users.Add(OrganisationAdminUsersData.MainOrganisationAdminUser);
        Context.Users.Add(AdminsData.MainAdmin);

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Q1 2026",
            description: null,
            startDate: QuarterStart,
            endDate: QuarterEnd);

        var existingModule = await Context.DataCollectionFormModules
            .FirstOrDefaultAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);

        if (existingModule != null)
        {
            _formModule = existingModule;
        }
        else
        {
            _formModule = DataCollectionFormModule.Create(
                id: DataCollectionFormModuleId.New(),
                code: DataCollectionFormModuleCodes.ServiceUsers,
                sectionNumber: 1,
                name: "Service Users",
                description: "Quarterly service users collection");
            Context.DataCollectionFormModules.Add(_formModule);
        }

        _dataCollection.AssignFormModule(_formModule.Id);
        Context.DataCollections.Add(_dataCollection);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static Service LiveService(string name)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, name);
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "0", "Live");
        return service;
    }
}