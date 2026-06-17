using System.Net.Http.Headers;
using System.Text;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class ValidateBulkUploadCommandTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private const string ServiceName = "Outcome Scores Service";

    private static readonly string[] OutcomeScoresHeaders =
    {
        "PPS01", "PPS02", "PPS03", "PPS04", "PPS05",
        "PPS06_pre", "PPS06_post", "PPS07_pre", "PPS07_post",
        "PPS08_pre", "PPS08_post", "PPS09_pre", "PPS09_post",
        "PPS10_pre", "PPS10_post", "PPS11_pre", "PPS11_post",
        "PPS12_pre", "PPS12_post", "PPS13_pre", "PPS13_post",
        "PPS14_pre", "PPS14_post", "PPS15_pre", "PPS15_post",
        "PPS17"
    };

    private Guid _moduleId;

    private static string BuildOutcomeScoresCsv(params string[] rows)
    {
        var lines = new List<string> { string.Join(",", OutcomeScoresHeaders) };
        lines.AddRange(rows);
        return string.Join("\n", lines);
    }

    private static string BuildDataRow(
        string pps02,
        string pps06Pre = "",
        string pps06Post = "",
        string pps08Pre = "",
        string pps08Post = "")
    {
        var cells = new Dictionary<string, string>
        {
            ["PPS01"] = ServiceName,
            ["PPS02"] = pps02,
            ["PPS03"] = "white",
            ["PPS04"] = "male",
            ["PPS05"] = "5",
            ["PPS06_pre"] = pps06Pre,
            ["PPS06_post"] = pps06Post,
            ["PPS07_pre"] = string.Empty,
            ["PPS07_post"] = string.Empty,
            ["PPS08_pre"] = pps08Pre,
            ["PPS08_post"] = pps08Post,
            ["PPS09_pre"] = string.Empty,
            ["PPS09_post"] = string.Empty,
            ["PPS10_pre"] = string.Empty,
            ["PPS10_post"] = string.Empty,
            ["PPS11_pre"] = string.Empty,
            ["PPS11_post"] = string.Empty,
            ["PPS12_pre"] = string.Empty,
            ["PPS12_post"] = string.Empty,
            ["PPS13_pre"] = string.Empty,
            ["PPS13_post"] = string.Empty,
            ["PPS14_pre"] = string.Empty,
            ["PPS14_post"] = string.Empty,
            ["PPS15_pre"] = string.Empty,
            ["PPS15_post"] = string.Empty,
            ["PPS17"] = string.Empty
        };

        return string.Join(",", OutcomeScoresHeaders.Select(k => EscapeCsv(cells[k])));
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    [Fact]
    public async Task ValidateBulkUpload_ReturnsNoErrorOnPps08_WhenPps02IsLabel()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));

        var result = await PostCsvAsync(csv);

        result.RowValidations.Should().HaveCount(1);
        var row = result.RowValidations[0];
        row.Errors.Should().NotContain(e => e.FieldCode.StartsWith("PPS08"));
        row.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBulkUpload_ReturnsNoErrorOnPps08_WhenPps02IsValueCode()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(pps02: "gad7", pps08Pre: "5", pps08Post: "6"));

        var result = await PostCsvAsync(csv);

        result.RowValidations[0].Errors.Should().NotContain(e => e.FieldCode.StartsWith("PPS08"));
        result.RowValidations[0].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBulkUpload_ReturnsNoErrorOnMultipleConditionalGroups_WhenPps02HasMixedLabelsAndValues()
    {
        var csv = BuildOutcomeScoresCsv(
            BuildDataRow(pps02: "ASQ-3, gad7", pps06Pre: "4", pps06Post: "5", pps08Pre: "5", pps08Post: "6"));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations[0];
        var errorSummary = string.Join("; ", row.Errors.Select(e => $"{e.FieldCode}={e.ErrorMessage}"));
        row.Errors.Should()
            .NotContain(e => e.FieldCode.StartsWith("PPS06"), errorSummary)
            .And.NotContain(e => e.FieldCode.StartsWith("PPS08"), errorSummary);
        row.IsValid.Should().BeTrue(errorSummary);
    }

    [Fact]
    public async Task ValidateBulkUpload_SkipsHiddenFieldValidation_WhenParentCheckboxDoesNotIncludeOption()
    {
        var csv = BuildOutcomeScoresCsv(
            BuildDataRow(pps02: "ASQ-3", pps06Pre: "4", pps06Post: "5"));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations[0];
        row.Errors.Should().NotContain(e => e.FieldCode.StartsWith("PPS08"));
        row.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBulkUpload_ReturnsFieldMetadata_WithPps02OptionsIncludingLabels()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));

        var result = await PostCsvAsync(csv);

        var pps02Metadata = result.FieldMetadata.FirstOrDefault(f => f.FieldCode == "PPS02");
        pps02Metadata.Should().NotBeNull();
        pps02Metadata!.Options.Should().Contain(o => o.Value == "gad7" && o.Label == "GAD-7");
    }

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        var service = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceName);
        Context.Services.Add(service);

        await SaveChangesAsync();

        var outcomeScoresModule = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.OutcomeScores);
        _moduleId = outcomeScoresModule.Id.Value;
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private async Task<BulkUploadValidationResultDto> PostCsvAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var submissionId = Guid.NewGuid();
        var url = $"organisation-admin/submissions/{submissionId}/modules/{_moduleId}/bulk-upload/validate";
        var response = await Client.PostAsync(url, content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadValidationResultDto>();
    }
}