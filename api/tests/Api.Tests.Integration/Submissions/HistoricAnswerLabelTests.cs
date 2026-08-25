using System.Net.Http.Json;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// BLUM-7614. Answers snapshot the question they were given under (BLUM-7178), but nothing read
/// that snapshot back: relabelling SPCP01 in Admin Configuration rewrote what an already-submitted
/// answer said.
///
/// Two halves. While the collection is still editable the module endpoint returns the live label
/// AND the snapshot, so the saved-answer summary can render history while the wizard shows the
/// current wording. Once the collection is locked the answer IS history, so the label itself
/// becomes the snapshot - the client routes a submitted module to the editable wizard, not the
/// summary, so leaving that to the client would miss the case the ticket is actually about.
/// </summary>
public class HistoricAnswerLabelTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private const string OriginalLabel = "Have you published information about your Healthy Babies offer?";
    private const string EditedLabel = "Have you published information about your Healthy Babies offer?22";

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _formModule = null!;

    [Fact]
    public async Task GetFormModule_ReturnsSnapshotLabel_AfterLiveQuestionRelabelled()
    {
        await RelabelLiveQuestionAsync();

        var field = await GetFieldAsync("SPCP01");

        field.QuestionLabel.Should().Be(
            OriginalLabel,
            "the historic answer must keep describing itself with the question it was actually answered under");
        field.Label.Should().Be(EditedLabel, "the live label still drives the edit form");
        field.DisplayValue.Should().Be("Yes - both physically and online");
    }

    // QA's exact script: fill in and save the module through the real endpoint (marking it complete,
    // so it re-opens as the read-only summary), then relabel the question in Admin Configuration.
    // The collection is never submitted, so nothing is locked.
    [Fact]
    public async Task GetFormModule_ReturnsSnapshotLabel_AfterSavingThroughTheEndpointThenRelabelling()
    {
        var saveUrl = $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_formModule.Id.Value}";
        var saveResponse = await Client.PostAsJsonAsync(saveUrl, new SaveFormModuleRequest(
            new Dictionary<string, string?> { ["SPCP01"] = "yes_both" },
            MarkComplete: true,
            CurrentStep: 1));
        saveResponse.IsSuccessStatusCode.Should().BeTrue(await saveResponse.Content.ReadAsStringAsync());

        await RelabelLiveQuestionAsync();

        var detail = await GetModuleAsync();
        detail.Status.Should().Be("Completed", "this is the state QA reported - the module is complete, the collection is not submitted");

        var field = detail.Fields.Single(f => f.Code == "SPCP01");
        field.QuestionLabel.Should().Be(OriginalLabel);
        field.Value.Should().Be("yes_both");

        // The wire contract the client actually reads. Deserialising into the DTO above would pass
        // even if the property were serialised under a name no client looks for.
        var url = $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_formModule.Id.Value}";
        var rawJson = await (await Client.GetAsync(url)).Content.ReadAsStringAsync();
        rawJson.Should().Contain("\"questionLabel\"", "client/src/helpers/answerSnapshot.ts reads field.questionLabel");
    }

    [Fact]
    public async Task GetFormModule_ReturnsSnapshotAsTheLabelItself_WhenCollectionIsLocked()
    {
        await LockCollectionAsync();
        await RelabelLiveQuestionAsync();

        var field = await GetFieldAsync("SPCP01");

        field.Label.Should().Be(
            OriginalLabel,
            "a submitted module opens in the wizard, which renders Label - so once the round is locked Label must itself be the historic wording");
        field.QuestionLabel.Should().Be(OriginalLabel);
        field.DisplayValue.Should().Be("Yes - both physically and online");
    }

    [Fact]
    public async Task GetFormModule_FallsBackToLiveLabel_WhenLockedAnswerHasNoSnapshot()
    {
        await LockCollectionAsync();

        var field = await GetFieldAsync("SPCP02");

        field.Label.Should().Be(
            "Do you run a Parent-Carer Panel?",
            "a legacy answer has no snapshot to fall back on, so the live label is all there is");
        field.QuestionLabel.Should().BeNull();
    }

    [Fact]
    public async Task GetFormModule_ReturnsNullSnapshotLabel_ForLegacyAnswer()
    {
        var field = await GetFieldAsync("SPCP02");

        field.QuestionLabel.Should().BeNull("this answer predates snapshotting, so callers fall back to the live label");
        field.Label.Should().Be("Do you run a Parent-Carer Panel?");
        field.Value.Should().Be("no");
    }

    private async Task<FormModuleFieldDto> GetFieldAsync(string code) =>
        (await GetModuleAsync()).Fields.Single(f => f.Code == code);

    private async Task<FormModuleDetailDto> GetModuleAsync()
    {
        var url = $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_formModule.Id.Value}";
        var response = await Client.GetAsync(url);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<FormModuleDetailDto>();
    }

    // Stands in for the local authority submitting the quarterly return. Deliberately SetStatus
    // rather than Submit: Submit stamps SubmittedById, which has a foreign key to the users table.
    private async Task LockCollectionAsync()
    {
        var submission = DataCollectionSubmission.Create(_dataCollection.Id, OrganisationAdminUsersData.OrganisationId);
        submission.SetStatus(DataCollectionSubmissionStatus.Submitted);
        Context.DataCollectionSubmissions.Add(submission);
        await SaveChangesAsync();
    }

    // Stands in for an admin renaming the question in Admin Configuration. Reloaded rather than
    // reused from InitializeAsync: SaveChangesAsync clears the change tracker, so the seeded
    // instance is detached and mutating it would never reach the database.
    private async Task RelabelLiveQuestionAsync()
    {
        var field = await Context.FormFields.SingleAsync(f => f.FormModuleId == _formModule.Id && f.FieldKey == "SPCP01");
        field.UpdateDetails(EditedLabel, field.DisplayOrder, field.IsRequired, field.Placeholder, field.HelpText, field.DefaultValue);
        await SaveChangesAsync();
    }

    public async Task InitializeAsync()
    {
        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        _formModule = DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: "historic-answer-label-test",
            sectionNumber: 1,
            name: "Healthy Babies offer and Parent-Carer Panels");

        var snapshotted = _formModule.AddField("SPCP01", OriginalLabel, FieldType.Radio, displayOrder: 1);
        snapshotted.AddOption("yes_both", "Yes - both physically and online", 1);
        snapshotted.AddOption("no", "No", 2);

        var legacy = _formModule.AddField("SPCP02", "Do you run a Parent-Carer Panel?", FieldType.Radio, displayOrder: 2);
        legacy.AddOption("yes", "Yes", 1);
        legacy.AddOption("no", "No", 2);

        Context.DataCollectionFormModules.Add(_formModule);

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Q1 2026",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30));
        _dataCollection.AssignLocalAuthority(OrganisationAdminUsersData.OrganisationId);
        _dataCollection.AssignFormModule(_formModule.Id);
        Context.DataCollections.Add(_dataCollection);

        var submission = FormSubmission.Create(
            _formModule.Id,
            OrganisationAdminUsersData.OrganisationId,
            _dataCollection.Id);

        submission.SetFieldValue(snapshotted, "yes_both");

        // The id-only overload is what every answer written before 20260727095439 went through,
        // and it captures nothing - this is the fallback case the read path has to keep handling.
        submission.SetFieldValue(legacy.Id, "no");

        Context.FormSubmissions.Add(submission);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}