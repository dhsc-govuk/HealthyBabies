using System.IO.Compression;
using System.Text;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

/// <summary>
/// BLUM-7614. The submission export writes a Question column per answer. It resolved the answer
/// itself from the snapshot but took the question text from the live form, so relabelling a
/// question in Admin Configuration retrospectively changed what every historic extract said an
/// answer was in response to.
/// </summary>
public class HistoricAnswerExportLabelTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private const string ModuleCode = "export-label-test";
    private const string OriginalLabel = "Have you published information about your Healthy Babies offer?";
    private const string EditedLabel = "Have you published information about your Healthy Babies offer?22";

    private readonly User _adminUser = AdminsData.MainAdmin;
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _formModule = null!;

    [Fact]
    public async Task DownloadSubmission_KeepsOriginalQuestionText_AfterLiveQuestionRelabelled()
    {
        var field = await Context.FormFields.SingleAsync(f => f.FormModuleId == _formModule.Id && f.FieldKey == "SPCP01");
        field.UpdateDetails(EditedLabel, field.DisplayOrder, field.IsRequired, field.Placeholder, field.HelpText, field.DefaultValue);
        await SaveChangesAsync();

        var response = await Client.GetAsync($"admin/data-collections/{_dataCollection.Id.Value}/local-authorities/{_organisation.Id.Value}/download?format=csv");

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var csv = await ReadCsvAsync(response, $"_{ModuleCode}.csv");

        csv.Should().Contain(OriginalLabel);
        csv.Should().NotContain(EditedLabel, "the extract describes what was answered, not what the form says today");
        csv.Should().Contain("Yes - both physically and online");
    }

    private static async Task<string> ReadCsvAsync(HttpResponseMessage response, string filenameSuffix)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entry = archive.Entries.First(e => e.FullName.EndsWith(filenameSuffix));
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public async Task InitializeAsync()
    {
        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_organisation);

        _formModule = DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: ModuleCode,
            sectionNumber: 1,
            name: "Healthy Babies offer and Parent-Carer Panels");

        var question = _formModule.AddField("SPCP01", OriginalLabel, FieldType.Radio, displayOrder: 1);
        question.AddOption("yes_both", "Yes - both physically and online", 1);
        question.AddOption("no", "No", 2);

        Context.DataCollectionFormModules.Add(_formModule);

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Export Label Test Collection",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30));

        _dataCollection.AssignLocalAuthority(_organisation.Id);
        _dataCollection.AssignFormModule(_formModule.Id);
        Context.DataCollections.Add(_dataCollection);

        var submission = FormSubmission.Create(_formModule.Id, _organisation.Id, _dataCollection.Id);
        submission.SetFieldValue(question, "yes_both");
        Context.FormSubmissions.Add(submission);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}