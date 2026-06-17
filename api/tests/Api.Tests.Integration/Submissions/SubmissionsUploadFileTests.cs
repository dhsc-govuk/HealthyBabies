using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class SubmissionsUploadFileTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private static readonly byte[] PdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A };

    private static readonly byte[] EicarBytes = Encoding.ASCII.GetBytes(
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    private Guid _moduleId;

    public async Task InitializeAsync()
    {
        await DataCollectionFormModuleSeeder.SeedAsync(Context);

        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);
        await SaveChangesAsync();

        var module = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.OutcomeScores);
        _moduleId = module.Id.Value;
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    [Fact]
    public async Task UploadFile_AcceptsCleanPdf()
    {
        var response = await UploadAsync(PdfHeader, "report.pdf", "application/pdf");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var dto = await response.ToResponseModel<FileUploadResultDto>();
        dto.FileName.Should().Be("report.pdf");
        dto.BlobUrl.Should().NotContain(
            "report.pdf",
            "blob path must use a randomised filename, not the user-supplied one");
        dto.BlobUrl.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task UploadFile_RejectsEicarTestFile()
    {
        var response = await UploadAsync(EicarBytes, "eicar.pdf", "application/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(
            "malware",
            because: "EICAR signature must trip the malware scanner");
    }

    [Fact]
    public async Task UploadFile_RejectsBlockedExtension()
    {
        var bytes = Encoding.ASCII.GetBytes("<?php echo system($_GET['cmd']); ?>");
        var response = await UploadAsync(bytes, "shell.php", "application/octet-stream");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(".php");
    }

    [Fact]
    public async Task UploadFile_RejectsDoubleExtensionBypass()
    {
        var bytes = Encoding.ASCII.GetBytes("<?php echo 1; ?>");
        var response = await UploadAsync(bytes, "shell.php.jpg", "image/jpeg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadFile_RejectsContentNotMatchingExtension()
    {
        var bytes = Encoding.ASCII.GetBytes("not really a pdf");
        var response = await UploadAsync(bytes, "report.pdf", "application/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadFile_RejectsEmptyFile()
    {
        var response = await UploadAsync(Array.Empty<byte>(), "empty.pdf", "application/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var submissionId = Guid.NewGuid();
        var url = $"organisation-admin/submissions/{submissionId}/modules/{_moduleId}/upload";
        return await Client.PostAsync(url, content);
    }
}