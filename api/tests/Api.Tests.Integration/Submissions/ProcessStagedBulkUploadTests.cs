using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Common.Interfaces;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Tests.Common.Services;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class ProcessStagedBulkUploadTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly OrganisationAdminIntegrationTestWebFactory _factory = factory;

    private const string ServiceName = "Outcome Scores Service";
    private const string OtherServiceName = "Other Service";

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
    private Guid _submissionId = Guid.NewGuid();

    [Fact]
    public async Task ValidateThenProcessStaged_PersistsRowsAndDeletesStagedBlob()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(serviceName: ServiceName, pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));

        var validation = await PostValidateAsync(csv);
        validation.StagingId.Should().NotBeEmpty();
        StagedBlobExists(validation.StagingId).Should().BeTrue();

        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { ServiceName },
            Array.Empty<BulkUploadCellEditDto>()));

        result.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);
        result.TotalRows.Should().Be(1);
        StagedBlobExists(validation.StagingId).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessStaged_AppliesCellEdits_BeforePersisting()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(serviceName: ServiceName, pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));
        var validation = await PostValidateAsync(csv);

        // PPS02 is at index 1 in OutcomeScoresHeaders
        var edits = new[] { new BulkUploadCellEditDto(RowIndex: 0, ColumnIndex: 1, Value: "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("ASQ-3"))) };

        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { ServiceName },
            edits));

        result.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessStaged_FiltersRowsByService_OnlyPersistsSelected()
    {
        var csv = BuildOutcomeScoresCsv(
            BuildDataRow(serviceName: ServiceName, pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"),
            BuildDataRow(serviceName: OtherServiceName, pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));

        var validation = await PostValidateAsync(csv);

        var result = await PostStagedAsync(new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { OtherServiceName },
            Array.Empty<BulkUploadCellEditDto>()));

        result.SuccessCount.Should().Be(1);
        result.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task ProcessStaged_ReturnsBadRequest_WhenStagingIdIsUnknown()
    {
        var unknownStagingId = Guid.NewGuid();

        var url = $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged";
        var response = await Client.PostAsJsonAsync(url, new ProcessStagedBulkUploadRequestDto(
            unknownStagingId,
            new[] { ServiceName },
            Array.Empty<BulkUploadCellEditDto>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Upload session expired");
    }

    [Fact]
    public async Task ProcessStaged_ReturnsBadRequest_WhenSelectedServicesIsEmpty()
    {
        var csv = BuildOutcomeScoresCsv(BuildDataRow(serviceName: ServiceName, pps02: "GAD-7", pps08Pre: "5", pps08Post: "6"));
        var validation = await PostValidateAsync(csv);

        var url = $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged";
        var response = await Client.PostAsJsonAsync(url, new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            Array.Empty<string>(),
            Array.Empty<BulkUploadCellEditDto>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        Context.Services.Add(Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceName));

        Context.Services.Add(Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: OtherServiceName));

        await SaveChangesAsync();

        var outcomeScoresModule = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.OutcomeScores);
        _moduleId = outcomeScoresModule.Id.Value;
        _submissionId = Guid.NewGuid();
    }

    public async Task DisposeAsync()
    {
        _factory.BlobService.Clear();
        await ClearAllTablesAsync();
    }

    private bool StagedBlobExists(Guid stagingId) =>
        _factory.BlobService.Exists("bulk-upload-staging", $"{stagingId}.csv");

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

    private static string BuildOutcomeScoresCsv(params string[] rows)
    {
        var lines = new List<string> { string.Join(",", OutcomeScoresHeaders) };
        lines.AddRange(rows);
        return string.Join("\n", lines);
    }

    private static string BuildDataRow(
        string serviceName,
        string pps02,
        string pps06Pre = "",
        string pps06Post = "",
        string pps08Pre = "",
        string pps08Post = "")
    {
        var cells = new Dictionary<string, string>
        {
            ["PPS01"] = serviceName,
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
            ["PPS17"] = string.Empty,
        };

        return string.Join(",", OutcomeScoresHeaders.Select(k => EscapeCsv(cells[k])));
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}