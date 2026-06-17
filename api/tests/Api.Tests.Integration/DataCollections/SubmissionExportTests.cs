using System.IO.Compression;
using System.Net;
using System.Text;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

public class SubmissionExportTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _standardFormModule = null!;
    private DataCollectionFormModule _serviceUsersFormModule = null!;

    [Fact]
    public async Task DownloadSubmission_ReturnsNotFound_WhenDataCollectionDoesNotExist()
    {
        var route = DownloadRoute(Guid.NewGuid(), _organisation.Id.Value);

        var response = await Client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadSubmission_ReturnsNotFound_WhenLocalAuthorityDoesNotExist()
    {
        var route = DownloadRoute(_dataCollection.Id.Value, Guid.NewGuid());

        var response = await Client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadSubmission_ReturnsZipFile_WithCsvEntryPerAssignedFormModule()
    {
        var response = await Client.GetAsync(DownloadRoute(_dataCollection.Id.Value, _organisation.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");

        var entries = await ReadZipEntries(response);
        entries.Should().HaveCount(2);
        entries.Should().Contain(e => e.EndsWith($"_{DataCollectionFormModuleCodes.HealthyBabies}.csv"));
        entries.Should().Contain(e => e.EndsWith($"_{DataCollectionFormModuleCodes.ServiceUsers}.csv"));
    }

    [Fact]
    public async Task DownloadSubmission_StandardFormCsv_DoesNotIncludeServiceIdColumn()
    {
        var response = await Client.GetAsync(DownloadRoute(_dataCollection.Id.Value, _organisation.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var headerRow = await ReadCsvHeaderRow(response, $"_{DataCollectionFormModuleCodes.HealthyBabies}.csv");

        headerRow.Should().Contain("LAName");
        headerRow.Should().Contain("ONSCode");
        headerRow.Should().Contain("CollectionPeriod");
        headerRow.Should().Contain("QuestionCode");
        headerRow.Should().NotContain("ServiceId");
    }

    [Fact]
    public async Task DownloadSubmission_ServiceUsersCsv_IncludesServiceIdColumn()
    {
        var response = await Client.GetAsync(DownloadRoute(_dataCollection.Id.Value, _organisation.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var headerRow = await ReadCsvHeaderRow(response, $"_{DataCollectionFormModuleCodes.ServiceUsers}.csv");

        headerRow.Should().Contain("LAName");
        headerRow.Should().Contain("ONSCode");
        headerRow.Should().Contain("CollectionPeriod");
        headerRow.Should().Contain("ServiceId");
    }

    public async Task InitializeAsync()
    {
        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_organisation);

        _standardFormModule = await GetOrCreateFormModuleAsync(DataCollectionFormModuleCodes.HealthyBabies, 1, "Healthy Babies");
        _serviceUsersFormModule = await GetOrCreateFormModuleAsync(DataCollectionFormModuleCodes.ServiceUsers, 2, "Service Users");

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Export Test Collection",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30));

        _dataCollection.AssignLocalAuthority(_organisation.Id);
        _dataCollection.AssignFormModule(_standardFormModule.Id);
        _dataCollection.AssignFormModule(_serviceUsersFormModule.Id);

        Context.DataCollections.Add(_dataCollection);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static string DownloadRoute(Guid dataCollectionId, Guid localAuthorityId) =>
        $"admin/data-collections/{dataCollectionId}/local-authorities/{localAuthorityId}/download?format=csv";

    private static async Task<IReadOnlyList<string>> ReadZipEntries(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    private static async Task<string> ReadCsvHeaderRow(HttpResponseMessage response, string filenameSuffix)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entry = archive.Entries.First(e => e.FullName.EndsWith(filenameSuffix));
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var firstLine = await reader.ReadLineAsync();
        return firstLine ?? string.Empty;
    }

    private async Task<DataCollectionFormModule> GetOrCreateFormModuleAsync(string code, int sectionNumber, string name)
    {
        var existing = await Context.DataCollectionFormModules.FirstOrDefaultAsync(m => m.Code == code);
        if (existing != null)
        {
            return existing;
        }

        var module = DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: code,
            sectionNumber: sectionNumber,
            name: name);

        Context.DataCollectionFormModules.Add(module);
        return module;
    }
}