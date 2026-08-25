using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// BLUM-7614. BulkUploadRowProcessor wrote activity answers through the SetFieldValue overload that
/// takes a field id, which captures no snapshot - so bulk-uploaded answers had no record of the
/// question or options they were given under and fell back to the live form forever, however many
/// times it was later edited. They must now snapshot exactly like a manually entered answer does.
/// </summary>
public class BulkUploadAnswerSnapshotTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly OrganisationAdminIntegrationTestWebFactory _factory = factory;

    private const string NameKey = "SNAP01";
    private const string StatusKey = "SNAP02";
    private const string StatusLabel = "What is the status of this activity?";

    private Guid _moduleId;
    private readonly Guid _submissionId = Guid.NewGuid();

    [Fact]
    public async Task BulkUpload_CapturesAnswerSnapshot_LikeManualEntry()
    {
        var csv = string.Join("\n", string.Join(",", NameKey, StatusKey), string.Join(",", "Bulk Uploaded Activity", "started"));

        var validation = await PostValidateAsync(csv);
        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { 0 },
            Array.Empty<BulkUploadCellEditDto>()));

        result.Success.Should().BeTrue(string.Join("; ", result.Results.Select(r => r.ErrorMessage)));

        var statusField = await Context.FormFields.FirstAsync(f => f.FormModuleId == new DataCollectionFormModuleId(_moduleId) && f.FieldKey == StatusKey);
        var submission = await Context.FormSubmissions
            .Include(s => s.FieldValues)
            .Where(s => s.FormModuleId == new DataCollectionFormModuleId(_moduleId) && s.EntityType == "Activity")
            .OrderByDescending(s => s.CreatedAt)
            .FirstAsync();

        var answer = submission.FieldValues.Single(v => v.FormFieldId == statusField.Id);

        answer.Value.Should().Be("started");
        answer.QuestionCode.Should().Be(StatusKey);
        answer.QuestionLabel.Should().Be(StatusLabel);
        answer.FieldType.Should().Be(FieldType.Radio.Value);
        answer.OptionsSnapshot.Should().NotBeNull();
        answer.OptionsSnapshot.Should().Contain("Started").And.Contain("Completed");
    }

    private async Task<BulkUploadValidationResultDto> PostValidateAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var response = await Client.PostAsync($"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/validate", content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadValidationResultDto>();
    }

    private async Task<BulkUploadResultDto> PostStagedAsync(ProcessStagedBulkUploadRequestDto request)
    {
        var response = await Client.PostAsJsonAsync($"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged", request);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadResultDto>();
    }

    public async Task InitializeAsync()
    {
        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            "bulk-upload-snapshot-test",
            1,
            "Bulk Upload Snapshot Test Module",
            allowsMultipleActivities: true);

        module.AddField(NameKey, "Activity name", FieldType.Textarea, 1, isRequired: true);
        var status = module.AddField(StatusKey, StatusLabel, FieldType.Radio, 2);
        status.AddOption("started", "Started", 1);
        status.AddOption("completed", "Completed", 2);

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();

        _moduleId = module.Id.Value;
    }

    public async Task DisposeAsync()
    {
        _factory.BlobService.Clear();
        await ClearAllTablesAsync();
    }
}