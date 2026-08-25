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
using Tests.Common.Services;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// Confirms or refutes, against the real bulk-upload endpoints and a real Postgres instance,
/// whether a Calculation-type field (e.g. "Planned Spend" on a Delivery Plan strand) gets a
/// value when an Activity is created via bulk-upload rather than the manual Activity form.
/// </summary>
public class ActivityBulkUploadCalculationFieldTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly OrganisationAdminIntegrationTestWebFactory _factory = factory;

    private const string NameKey = "CALC02";
    private const string ForecastKey = "CALC03";
    private const string ActualKey = "CALC04";
    private const string CommittedKey = "CALC05";
    private const string PlannedKey = "CALC06";

    private Guid _moduleId;
    private readonly Guid _submissionId = Guid.NewGuid();

    [Fact]
    public async Task BulkUpload_ForActivityModule_PopulatesCalculationField_ToMatchManualEntryBehaviour()
    {
        var csv = BuildCsv(name: "Bulk Uploaded Activity", forecast: "10000", actual: "2000", committed: "1000");

        var validation = await PostValidateAsync(csv);
        validation.StagingId.Should().NotBeEmpty();

        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { 0 },
            Array.Empty<BulkUploadCellEditDto>()));

        result.Success.Should().BeTrue(string.Join("; ", result.Results.Select(r => r.ErrorMessage)));
        result.SuccessCount.Should().Be(1);

        var submission = await Context.FormSubmissions
            .Include(s => s.FieldValues)
            .Where(s => s.FormModuleId == new DataCollectionFormModuleId(_moduleId) && s.EntityType == "Activity")
            .OrderByDescending(s => s.CreatedAt)
            .FirstAsync();

        var plannedField = await Context.FormFields.FirstAsync(f => f.FormModuleId == new DataCollectionFormModuleId(_moduleId) && f.FieldKey == PlannedKey);
        var plannedValue = submission.FieldValues.FirstOrDefault(v => v.FormFieldId == plannedField.Id)?.Value;

        // Expected (matching manual-entry behaviour and the formula "CALC03 - (CALC04 + CALC05)"):
        // 10000 - (2000 + 1000) = 7000
        var reason = "the Calculation field should auto-populate the same way it does when a person fills in the form " +
            "manually in the browser, not stay blank just because it came in via CSV instead";
        plannedValue.Should().Be("7000", reason);
    }

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            "calc-activity-test",
            1,
            "Calculation Field Test Module",
            allowsMultipleActivities: true);

        module.AddField("CALC01", "Activity ID", FieldType.ReadOnly, 1);
        module.AddField(NameKey, "Activity name", FieldType.Textarea, 2, isRequired: true);
        module.AddField(ForecastKey, "Forecasted spend", FieldType.Currency, 3, isRequired: true);
        module.AddField(ActualKey, "Actual spend", FieldType.Currency, 4);
        module.AddField(CommittedKey, "Committed spend", FieldType.Currency, 5);
        var planned = module.AddField(PlannedKey, "Planned spend (uncommitted)", FieldType.Calculation, 6);
        planned.SetConfiguration($"{{\"formula\":\"{ForecastKey} - ({ActualKey} + {CommittedKey})\",\"prefix\":\"£\"}}");

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();

        _moduleId = module.Id.Value;
    }

    public async Task DisposeAsync()
    {
        _factory.BlobService.Clear();
        await ClearAllTablesAsync();
    }

    private static string BuildCsv(string name, string forecast, string actual, string committed)
    {
        var headers = string.Join(",", NameKey, ForecastKey, ActualKey, CommittedKey);
        var row = string.Join(",", name, forecast, actual, committed);
        return string.Join("\n", headers, row);
    }

    private async Task<BulkUploadValidationResultDto> PostValidateAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var url = $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/validate";
        var response = await Client.PostAsync(url, content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadValidationResultDto>();
    }

    private async Task<BulkUploadResultDto> PostStagedAsync(ProcessStagedBulkUploadRequestDto request)
    {
        var url = $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged";
        var response = await Client.PostAsJsonAsync(url, request);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadResultDto>();
    }
}