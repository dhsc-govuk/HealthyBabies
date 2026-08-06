using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Controllers;
using Application.Services.Dtos;
using Application.Services.Queries;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Services;

public class ServicesBulkUploadControllerTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly OrganisationAdminIntegrationTestWebFactory _factory = factory;

    private const string StagingContainer = "services-bulk-upload-staging";

    // A complete, non-conditional-triggering set of answers for every currently-required question -
    // SMD03=0 (Live) keeps SMD04 out of scope, SMD07=false keeps SMD08 out of scope, SMD09=0 keeps
    // SMD10 out of scope, so none of the conditionally-required questions apply to this row.
    private static readonly string[] Headers =
    {
        "SMD01", "SMD02", "SMD03", "SMD05", "SMD07", "SMD09",
        "SMD12", "SMD13", "SMD14", "SMD15", "SMD16", "SMD17", "SMD18", "SMD20", "SMD21"
    };

    private Service _service1 = null!;
    private Service _service2 = null!;
    private Service _serviceWithAnswers = null!;

    [Fact]
    public async Task ShouldDownloadTemplate_ReturnsCsvWithQuestionCodes()
    {
        // Act
        var response = await Client.GetAsync("services/bulk-upload/template");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First row should contain question codes
        lines[0].Should().Contain("SMD01");
        lines[0].Should().Contain("SMD02");
        lines[0].Should().Contain("SMD14");

        // Second row should contain labels
        lines[1].Should().Contain("What is the service name?");
        lines[1].Should().Contain("Is the service funded by the BSFH&HB programme?");
        lines[1].Should().Contain("Is this a targeted, specialist or universal service?");
    }

    [Fact]
    public async Task ShouldMatchServices_ReturnsMatchedAndUnmatchedServices()
    {
        // Arrange
        var request = new MatchServicesRequest(new List<string> { "Service One", "Non Existent Service" });

        // Act
        var response = await Client.PostAsJsonAsync("services/bulk-upload/match", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var results = await response.ToResponseModel<List<ServiceMatchResult>>();

        results.Should().HaveCount(2);

        // Check matched service
        var matchedResult = results.First(r => r.SearchName == "Service One");
        matchedResult.ServiceId.Should().Be(_service1.Id.Value);
        matchedResult.MatchedName.Should().Be("Service One");

        // Check unmatched service
        var unmatchedResult = results.First(r => r.SearchName == "Non Existent Service");
        unmatchedResult.ServiceId.Should().BeNull();
        unmatchedResult.MatchedName.Should().BeNull();
    }

    [Fact]
    public async Task ShouldBulkUpload_ValidatesThenPersistsAnswers_ForExistingServiceMatchedByName()
    {
        // Arrange - "Service One" already exists, matching by name should update it rather than create a new one
        var csv = BuildCsv(BuildDataRow(serviceName: "Service One"));

        // Act
        var validation = await PostValidateAsync(csv);
        validation.IsValid.Should().BeTrue();
        StagedBlobExists(validation.StagingId).Should().BeTrue();

        var result = await PostStagedAsync(validation.StagingId, new[] { 2 });

        // Assert
        result.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);
        result.Results.Single().IsNew.Should().BeFalse();
        result.Results.Single().ServiceId.Should().Be(_service1.Id.Value);
        StagedBlobExists(validation.StagingId).Should().BeFalse();

        var smd02Answer = Context.ServiceAnswers.FirstOrDefault(a => a.ServiceId == _service1.Id && a.QuestionCode == "SMD02");
        smd02Answer.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldBulkUpload_PersistsMultipleRows()
    {
        // Arrange
        var csv = BuildCsv(
            BuildDataRow(serviceName: "Service One"),
            BuildDataRow(serviceName: "Service Two"));

        // Act
        var validation = await PostValidateAsync(csv);
        var result = await PostStagedAsync(validation.StagingId, new[] { 2, 3 });

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);

        Context.ServiceAnswers.Any(a => a.ServiceId == _service1.Id && a.QuestionCode == "SMD02").Should().BeTrue();
        Context.ServiceAnswers.Any(a => a.ServiceId == _service2.Id && a.QuestionCode == "SMD02").Should().BeTrue();
    }

    [Fact]
    public async Task ShouldBulkUpload_ClearsExistingAnswers_BeforeApplyingNewOnes()
    {
        // Arrange - service with a pre-existing SMD14 answer that isn't in the new upload's value
        var csv = BuildCsv(BuildDataRow(serviceName: "Service With Answers", smd14: "1"));

        // Act
        var validation = await PostValidateAsync(csv);
        await PostStagedAsync(validation.StagingId, new[] { 2 });

        // Assert - the old SMD14 answer ("0" / "Targeted") is replaced, not left alongside the new one
        var smd14Answers = Context.ServiceAnswers
            .Where(a => a.ServiceId == _serviceWithAnswers.Id && a.QuestionCode == "SMD14")
            .ToList();

        smd14Answers.Should().HaveCount(1);
        smd14Answers.Single().DisplayValue.Should().Be("Specialist");
    }

    [Fact]
    public async Task ShouldBulkUpload_MapsCheckboxValuesToDisplayLabels()
    {
        // Arrange
        var csv = BuildCsv(BuildDataRow(serviceName: "Service One", smd14: "0,1"));

        // Act
        var validation = await PostValidateAsync(csv);
        await PostStagedAsync(validation.StagingId, new[] { 2 });

        // Assert
        var checkboxAnswer = Context.ServiceAnswers
            .FirstOrDefault(a => a.ServiceId == _service1.Id && a.QuestionCode == "SMD14");

        checkboxAnswer.Should().NotBeNull();
        checkboxAnswer!.DisplayValue.Should().Be("Targeted, Specialist");
    }

    [Fact]
    public async Task ShouldBulkUpload_SetsServiceStatusToComplete()
    {
        // Arrange
        var csv = BuildCsv(BuildDataRow(serviceName: "Service One"));

        // Act
        var validation = await PostValidateAsync(csv);
        await PostStagedAsync(validation.StagingId, new[] { 2 });

        // Assert
        await Context.Entry(_service1).ReloadAsync();
        _service1.Status.Should().Be(ServiceStatus.Complete);
        _service1.CurrentStep.Should().Be(3);
    }

    [Fact]
    public async Task ShouldBulkUpload_ReturnsPartialSuccess_WhenARowIsADuplicateOfAnotherInTheSameUpload()
    {
        // Arrange - two rows share the same (new) service name within the same upload
        var csv = BuildCsv(
            BuildDataRow(serviceName: "Brand New Service"),
            BuildDataRow(serviceName: "Brand New Service"));

        // Act
        var validation = await PostValidateAsync(csv);
        validation.IsValid.Should().BeFalse();
        validation.RowValidations.Should().Contain(r => !r.IsValid && r.RowNumber == 3);

        var result = await PostStagedAsync(validation.StagingId, new[] { 2, 3 });

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);

        var failedRow = result.Results.First(r => !r.IsSuccess);
        failedRow.ErrorMessage.Should().Contain("Duplicate");
    }

    public async Task InitializeAsync()
    {
        await ServiceFormQuestionSeeder.SeedAsync(Context);

        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        _service1 = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service One");
        Context.Services.Add(_service1);

        _service2 = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service Two");
        Context.Services.Add(_service2);

        _serviceWithAnswers = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: "Service With Answers");
        _serviceWithAnswers.AddAnswer(
            "SMD14",
            "Is this a targeted, specialist or universal service?",
            null,
            ServiceFormQuestionType.Checkbox,
            2,
            8,
            "0",
            "Targeted");
        Context.Services.Add(_serviceWithAnswers);

        _ = await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        _factory.BlobService.Clear();
        await ClearAllTablesAsync();
    }

    private bool StagedBlobExists(Guid stagingId)
    {
        var extensionedNames = new[] { $"{stagingId}/upload.csv", $"{stagingId}/upload.xlsx" };
        return extensionedNames.Any(name => _factory.BlobService.Exists(StagingContainer, name));
    }

    private async Task<BulkUploadServicesValidationResultDto> PostValidateAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var response = await Client.PostAsync("services/bulk-upload/validate", content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadServicesValidationResultDto>();
    }

    private async Task<BulkUploadServicesResultDto> PostStagedAsync(Guid stagingId, int[] selectedRowNumbers)
    {
        var request = new ProcessStagedBulkUploadServicesRequest(stagingId, selectedRowNumbers, Array.Empty<BulkUploadServiceCellEditRequest>());
        var response = await Client.PostAsJsonAsync("services/bulk-upload/staged", request);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadServicesResultDto>();
    }

    private static string BuildCsv(params string[] rows)
    {
        var lines = new List<string> { string.Join(",", Headers) };
        lines.AddRange(rows);
        return string.Join("\n", lines);
    }

    private static string BuildDataRow(
        string serviceName,
        string smd02 = "0",
        string smd14 = "0")
    {
        var cells = new Dictionary<string, string>
        {
            ["SMD01"] = serviceName,
            ["SMD02"] = smd02,
            ["SMD03"] = "0",
            ["SMD05"] = "0",
            ["SMD07"] = "false",
            ["SMD09"] = "0",
            ["SMD12"] = "0",
            ["SMD13"] = "0",
            ["SMD14"] = smd14,
            ["SMD15"] = "0",
            ["SMD16"] = "0",
            ["SMD17"] = "all_hub_sites",
            ["SMD18"] = "0",
            ["SMD20"] = "0",
            ["SMD21"] = "0",
        };

        return string.Join(",", Headers.Select(h => EscapeCsvValue(cells[h])));
    }

    private static string EscapeCsvValue(string value) =>
        value.Contains(',') ? $"\"{value}\"" : value;
}