using System.Net.Http.Headers;
using System.Text;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

// BLUM-7184. The validate endpoint checked one cell at a time and never ran the cross-field
// demographic rule, so a row whose ethnicity boxes added up to more than the QSU03 total came back
// valid: the only thing standing between it and a saved submission was the browser.
public class ValidateBulkUploadServiceUsersTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private const string ServiceName = "Test Service - Family Hub Support";
    private static readonly Guid DataCollectionGuid = new("c3d4e5f6-a7b8-9012-cdef-123456789012");

    // Everything the row needs answered, plus the ethnicity boxes under test. Blank columns are left
    // out entirely - the row is mapped by header, so an absent column reads the same as an empty one.
    private static readonly string[] Headers =
    {
        "ServiceName", "QSU01", "QSU02", "QSU03", "QSU05", "QSU06", "QSU07",
        "QSU08_white", "QSU08_mixed", "QSU08_asian", "QSU08_black", "QSU08_other",
        "QSU09", "QSU11", "QSU13A", "QSU13", "QSU17"
    };

    private Guid _moduleId;

    [Fact]
    public async Task ValidateBulkUpload_RejectsRow_WhenEthnicitySubTotalsExceedQsu03()
    {
        // 10 + 6 + 6 + 4 + 2 = 28 people by ethnicity, against 23 in total.
        var csv = BuildCsv(Row(white: "10", mixed: "6", asian: "6", black: "4", other: "2"));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations.Should().ContainSingle().Subject;
        var summary = string.Join("; ", row.Errors.Select(e => $"{e.FieldCode}={e.ErrorMessage}"));

        row.Errors.Should().Contain(
            e => e.FieldCode.StartsWith("QSU08") && e.ErrorMessage.Contains("(28)") && e.ErrorMessage.Contains("cannot exceed the total number of service users (23)"),
            summary);
        row.IsValid.Should().BeFalse(summary);
        result.IsValid.Should().BeFalse();
        result.InvalidRows.Should().Be(1);
    }

    [Fact]
    public async Task ValidateBulkUpload_NamesEveryContributingEthnicityColumn_WhenSubTotalsExceedQsu03()
    {
        var csv = BuildCsv(Row(white: "10", mixed: "6", asian: "6", black: "4", other: "2"));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations.Should().ContainSingle().Subject;
        row.Errors.Select(e => e.FieldCode).Should().Contain(new[]
        {
            "QSU08_white", "QSU08_mixed", "QSU08_asian", "QSU08_black", "QSU08_other",
        });
    }

    [Fact]
    public async Task ValidateBulkUpload_AcceptsRow_WhenEthnicitySubTotalsAreWithinQsu03()
    {
        // 10 + 6 + 4 + 2 + 1 = 23, exactly the total.
        var csv = BuildCsv(Row(white: "10", mixed: "6", asian: "4", black: "2", other: "1"));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations.Should().ContainSingle().Subject;
        var summary = string.Join("; ", row.Errors.Select(e => $"{e.FieldCode}={e.ErrorMessage}"));

        row.IsValid.Should().BeTrue(summary);
        result.IsValid.Should().BeTrue(summary);
    }

    [Fact]
    public async Task ValidateBulkUpload_AcceptsRow_WhenQsu17IsBlank()
    {
        var csv = BuildCsv(Row(white: "10", mixed: "6", asian: "4", black: "2", other: "1", qsu17: string.Empty));

        var result = await PostCsvAsync(csv);

        var row = result.RowValidations.Should().ContainSingle().Subject;
        row.Errors.Should().NotContain(e => e.FieldCode == "QSU17");
        row.IsValid.Should().BeTrue(string.Join("; ", row.Errors.Select(e => $"{e.FieldCode}={e.ErrorMessage}")));
    }

    [Fact]
    public async Task ValidateBulkUpload_ReturnsServiceNameAsTheNameFieldCode()
    {
        var csv = BuildCsv(Row(white: "1", mixed: "1", asian: "1", black: "1", other: "1"));

        var result = await PostCsvAsync(csv);

        result.NameFieldCode.Should().Be("ServiceName");
    }

    // Without the name column the wizard can match no row to a service, which used to leave its
    // "choose services you want to update" step with an empty list and a Confirm button disabled for
    // good. Rejecting the file here is what puts the user back on the upload step with a reason.
    [Fact]
    public async Task ValidateBulkUpload_ReturnsModuleError_WhenServiceNameColumnIsMissing()
    {
        var headers = Headers.Skip(1).ToArray();
        var cells = RowCells(white: "1", mixed: "1", asian: "1", black: "1", other: "1", qsu17: string.Empty);
        var csv = string.Join("\n", new[]
        {
            string.Join(",", headers),
            string.Join(",", headers.Select(h => EscapeCsv(cells[h]))),
        });

        var result = await PostCsvAsync(csv);

        result.ModuleError.Should().NotBeNull();
        result.ModuleError.Should().Contain("ServiceName");
        result.IsValid.Should().BeFalse();
    }

    private static Dictionary<string, string> RowCells(
        string white,
        string mixed,
        string asian,
        string black,
        string other,
        string qsu17 = "") =>
        new()
        {
            ["ServiceName"] = ServiceName,
            ["QSU01"] = "yes",
            ["QSU02"] = "yes",
            ["QSU03"] = "23",
            ["QSU05"] = "yes",
            ["QSU06"] = "primary_carer",
            ["QSU07"] = "yes",
            ["QSU08_white"] = white,
            ["QSU08_mixed"] = mixed,
            ["QSU08_asian"] = asian,
            ["QSU08_black"] = black,
            ["QSU08_other"] = other,
            ["QSU09"] = "no",
            ["QSU11"] = "no",
            ["QSU13A"] = "no",
            ["QSU13"] = "no",
            ["QSU17"] = qsu17,
        };

    private static string Row(
        string white,
        string mixed,
        string asian,
        string black,
        string other,
        string qsu17 = "")
    {
        var cells = RowCells(white, mixed, asian, black, other, qsu17);
        return string.Join(",", Headers.Select(h => EscapeCsv(cells[h])));
    }

    private static string BuildCsv(params string[] rows)
    {
        var lines = new List<string> { string.Join(",", Headers) };
        lines.AddRange(rows);
        return string.Join("\n", lines);
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        Context.Organisations.Add(Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true));

        var service = Service.New(
            id: ServiceId.New(),
            organisationId: OrganisationAdminUsersData.OrganisationId,
            name: ServiceName);

        // QSU07 and the other demographic gates are conditional on the Service being programme
        // funded (SMD02), so without this the ethnicity question is never asked of this row.
        service.AddAnswer("SMD02", "Is the service funded by the BSFH&HB programme?", null, ServiceFormQuestionType.Radio, 1, 2, "0", "Funded");
        Context.Services.Add(service);

        await SaveChangesAsync();

        var serviceUsersModule = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
        _moduleId = serviceUsersModule.Id.Value;
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private async Task<BulkUploadValidationResultDto> PostCsvAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "upload.csv");

        var url = $"organisation-admin/submissions/{DataCollectionGuid}/modules/{_moduleId}/bulk-upload/validate";
        var response = await Client.PostAsync(url, content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.ToResponseModel<BulkUploadValidationResultDto>();
    }
}