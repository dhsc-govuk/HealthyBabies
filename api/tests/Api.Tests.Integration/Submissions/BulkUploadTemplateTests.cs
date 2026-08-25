using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Submissions.Dtos;
using Application.Submissions.Helpers;
using ClosedXML.Excel;
using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// BLUM-7463: the downloaded template must reflect form-builder option changes. Before the fix the
/// CSV carried only field keys and labels, so adding an option to QSU06 could not change the file.
/// </summary>
public class BulkUploadTemplateTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private const string ServiceName = "Service Users Service";
    private static readonly Guid DataCollectionGuid = new("b2c3d4e5-f6a7-8901-bcde-f23456789012");

    private Guid _moduleId;

    [Fact]
    public async Task DownloadCsvTemplate_ListsCurrentOptionValues_ForEachQuestion()
    {
        var rows = SplitRows(await DownloadCsvTemplateAsync());

        var optionsRow = rows[2];
        var qsu06 = optionsRow[IndexOf(rows[0], "QSU06")];

        qsu06.Should().StartWith(BulkUploadOptionsRow.CellPrefix);
        qsu06.Should().Contain("primary_carer").And.Contain("unknown");

        // QSU06 is a checkbox, so the column takes several values at once
        qsu06.Should().Contain("comma-separated");

        // A single-select question lists its values without the multi-value hint
        var qsu05 = optionsRow[IndexOf(rows[0], "QSU05")];
        qsu05.Should().Be($"{BulkUploadOptionsRow.CellPrefix} yes|no");

        // Free-text and numeric questions have nothing to list
        optionsRow[IndexOf(rows[0], "QSU03")].Should().BeEmpty();
        optionsRow[0].Should().BeEmpty("the ServiceName column is not an option list");
    }

    [Fact]
    public async Task DownloadCsvTemplate_IncludesNewlyAddedOption_WhenQuestionOptionsChange()
    {
        var before = await DownloadCsvTemplateAsync();
        before.Should().NotContain("test_option");

        await AddQsu06OptionAsync("test_option", "Test option");

        var after = await DownloadCsvTemplateAsync();

        after.Should().NotBe(before);
        var optionsRow = SplitRows(after)[2];
        optionsRow[IndexOf(SplitRows(after)[0], "QSU06")].Should().Contain("test_option");
    }

    [Fact]
    public async Task ValidateBulkUpload_SkipsOptionsRow_WhenTemplateIsUploadedUnchanged()
    {
        // The template already carries one row per incomplete service, so it round-trips as-is
        var result = await PostCsvAsync(await DownloadCsvTemplateAsync());

        result.TotalRows.Should().Be(1, "the labels and options rows are documentation, not data");
        result.RowValidations.Should().HaveCount(1);
        result.RowValidations[0].RowNumber.Should().Be(4, "the data starts on CSV line 4");
        result.RowValidations[0].EntityName.Should().Be(ServiceName);
    }

    [Fact]
    public async Task DownloadExcelTemplate_PutsOptionsOnRowThree_AndShiftsDataDown()
    {
        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{_moduleId}/bulk-upload/template?format=xlsx";
        var response = await Client.GetAsync(url);
        response.IsSuccessStatusCode.Should().BeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        var sheet = workbook.Worksheet("Data");

        var qsu06Column = ColumnOf(sheet, "QSU06");
        sheet.Cell(3, qsu06Column).GetString().Should().Contain("primary_carer").And.Contain("comma-separated");

        // Users fill this sheet then "Save As CSV", so the pre-filled rows have to move below it
        sheet.Cell(4, 1).GetString().Should().Be(ServiceName);

        // Dropdowns must cover the data rows, not the options row that now sits above them
        var qsu01Column = ColumnOf(sheet, "QSU01");
        var qsu01Ranges = sheet.DataValidations
            .SelectMany(v => v.Ranges)
            .Where(r => r.RangeAddress.FirstAddress.ColumnNumber == qsu01Column)
            .ToList();

        qsu01Ranges.Should().NotBeEmpty("single-select columns keep their dropdown");
        qsu01Ranges.Should().OnlyContain(r => r.RangeAddress.FirstAddress.RowNumber == 4);

        // The options cell is far longer than its label, so auto-fit has to stay bounded or the
        // sheet becomes unusably wide to scroll
        sheet.Column(qsu06Column).Width.Should().BeLessThanOrEqualTo(60);
    }

    [Fact]
    public async Task ProcessStaged_AppliesCellEditToTheRightRow_WhenTemplateHasAnOptionsRow()
    {
        var template = await DownloadCsvTemplateAsync();
        var validation = await PostCsvAsync(template);

        // The client addresses rows by their index among the data rows, so the extra preamble row
        // has to be skipped identically on validate and process or edits land on the wrong record.
        var qsu01Column = IndexOf(SplitRows(template)[0], "QSU01");
        var edits = new[] { new BulkUploadCellEditDto(RowIndex: 0, ColumnIndex: qsu01Column, Value: Encode("no")) };

        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { 0 },
            edits));

        result.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);

        var submission = await Context.FormSubmissions
            .Include(s => s.FieldValues)
            .FirstAsync(s => s.FormModuleId == new DataCollectionFormModuleId(_moduleId));

        var qsu01 = await Context.FormFields.FirstAsync(f => f.FieldKey == "QSU01");
        submission.FieldValues.First(v => v.FormFieldId == qsu01.Id).Value.Should().Be("no");
    }

    [Fact]
    public async Task ActivityTemplate_RoundTrips_WithTheSameColumnsValidateAndProcessAgreeOn()
    {
        var moduleId = await SeedActivityModuleAsync();

        var template = await DownloadCsvTemplateAsync(moduleId);
        var rows = SplitRows(template);

        // Activity modules auto-generate ACT01 and auto-total ACT04, so neither is a column
        rows[0].Should().BeEquivalentTo(new[] { "ACT02", "ACT03" });
        rows[2][1].Should().StartWith(BulkUploadOptionsRow.CellPrefix).And.Contain("capital");

        var validation = await PostCsvAsync($"{template}Playgroup,capital", moduleId);
        validation.TotalRows.Should().Be(1);
        validation.RowValidations[0].RowNumber.Should().Be(4);

        var request = new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { 0 },
            Array.Empty<BulkUploadCellEditDto>());
        var result = await PostStagedAsync(request, moduleId);

        result.SuccessCount.Should().Be(1);

        var submission = await Context.FormSubmissions
            .Include(s => s.FieldValues)
            .FirstAsync(s => s.FormModuleId == new DataCollectionFormModuleId(moduleId));

        // The processor still needs the columns the template leaves out — it generates the activity
        // ID from the ReadOnly field, so narrowing its field list to the template's would break this
        var activityIdField = await Context.FormFields
            .FirstAsync(f => f.FormModuleId == new DataCollectionFormModuleId(moduleId) && f.FieldKey == "ACT01");

        submission.FieldValues.Should().Contain(v => v.FormFieldId == activityIdField.Id);
    }

    [Fact]
    public async Task DownloadCsvTemplate_OmitsOptionsRow_WhenNoQuestionHasOptions()
    {
        var moduleId = await SeedTextOnlyModuleAsync();

        var rows = SplitRows(await DownloadCsvTemplateAsync(moduleId));

        // A row of blank cells isn't skipped on upload, so it must not be written at all
        rows.Should().HaveCount(3, "header, labels, and the one pre-filled service row");
        rows[2][0].Should().Be(ServiceName);
    }

    [Fact]
    public async Task ValidateBulkUpload_StillSkipsLabelsRowOnly_WhenTemplatePredatesTheOptionsRow()
    {
        var rows = SplitRows(await DownloadCsvTemplateAsync());
        var headerLine = string.Join(",", rows[0]);
        var labelLine = string.Join(",", rows[1].Select(EscapeCsv));
        var dataRow = string.Join(",", rows[0].Select(h => h == "ServiceName" ? ServiceName : string.Empty));

        var result = await PostCsvAsync($"{headerLine}\n{labelLine}\n{dataRow}");

        result.TotalRows.Should().Be(1);
        result.RowValidations[0].RowNumber.Should().Be(3, "an older template has no options row to skip");
    }

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        Context.Services.Add(Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceName));

        await SaveChangesAsync();

        var serviceUsersModule = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
        _moduleId = serviceUsersModule.Id.Value;
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private async Task AddQsu06OptionAsync(string value, string label)
    {
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .ThenInclude(f => f.Options)
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);

        var qsu06 = module.Fields.First(f => f.FieldKey == "QSU06");
        qsu06.AddOption(value, label, qsu06.Options.Count + 1);

        await SaveChangesAsync();
    }

    private async Task<Guid> SeedActivityModuleAsync()
    {
        var module = DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: "activity-template-module",
            sectionNumber: 98,
            name: "Activity Template Module",
            allowsMultipleActivities: true);

        module.AddField("ACT01", "Activity ID", FieldType.ReadOnly, 1);
        module.AddField("ACT02", "Activity name", FieldType.Textarea, 2, isRequired: true);
        var fundingType = module.AddField("ACT03", "Funding type", FieldType.Radio, 3);
        fundingType.AddOption("capital", "Capital", 1);
        fundingType.AddOption("revenue", "Revenue", 2);
        module.AddField("ACT04", "Total", FieldType.Calculation, 4);

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();

        return module.Id.Value;
    }

    private async Task<Guid> SeedTextOnlyModuleAsync()
    {
        var module = DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: "text-only-module",
            sectionNumber: 99,
            name: "Text Only Module");

        module.AddField("TXT01", "A question with nothing to pick from", FieldType.Text, 1);

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();

        return module.Id.Value;
    }

    private async Task<string> DownloadCsvTemplateAsync() => await DownloadCsvTemplateAsync(_moduleId);

    private async Task<string> DownloadCsvTemplateAsync(Guid moduleId)
    {
        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{moduleId}/bulk-upload/template?format=csv";
        var response = await Client.GetAsync(url);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<BulkUploadResultDto> PostStagedAsync(ProcessStagedBulkUploadRequestDto request) => await PostStagedAsync(request, _moduleId);

    private async Task<BulkUploadResultDto> PostStagedAsync(ProcessStagedBulkUploadRequestDto request, Guid moduleId)
    {
        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{moduleId}/bulk-upload/staged";
        var response = await Client.PostAsJsonAsync(url, request);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadResultDto>();
    }

    private static string Encode(string value) =>
        "b64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private async Task<BulkUploadValidationResultDto> PostCsvAsync(string csv) => await PostCsvAsync(csv, _moduleId);

    private async Task<BulkUploadValidationResultDto> PostCsvAsync(string csv, Guid moduleId)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{moduleId}/bulk-upload/validate";
        var response = await Client.PostAsync(url, content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadValidationResultDto>();
    }

    private static List<List<string>> SplitRows(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => ParseCsvLine(line.TrimEnd('\r')))
            .ToList();

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static int ColumnOf(IXLWorksheet sheet, string fieldKey)
    {
        var cell = sheet.Row(1).Cells().FirstOrDefault(c => c.GetString() == fieldKey);
        cell.Should().NotBeNull($"the Data sheet should have a {fieldKey} column");
        return cell!.Address.ColumnNumber;
    }

    private static int IndexOf(List<string> headers, string fieldKey)
    {
        var index = headers.IndexOf(fieldKey);
        index.Should().BeGreaterThanOrEqualTo(0, $"the template should have a {fieldKey} column");
        return index;
    }
}