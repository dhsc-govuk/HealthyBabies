using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Common.Constants;
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
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.RequestStaging;

public class RequestBodyRehydrationTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private readonly OrganisationAdminIntegrationTestWebFactory _factory = factory;

    private const string ServiceName = "Outcome Scores Service";

    private static readonly string[] OutcomeScoresHeaders =
    {
        "PPS01", "PPS02", "PPS03", "PPS04", "PPS05",
        "PPS06_pre", "PPS06_post", "PPS07_pre", "PPS07_post",
        "PPS08_pre", "PPS08_post", "PPS09_pre", "PPS09_post",
        "PPS10_pre", "PPS10_post", "PPS11_pre", "PPS11_post",
        "PPS12_pre", "PPS12_post", "PPS13_pre", "PPS13_post",
        "PPS14_pre", "PPS14_post", "PPS15_pre", "PPS15_post",
        "PPS17",
    };

    private Guid _moduleId;
    private Guid _submissionId = Guid.NewGuid();

    [Fact]
    public async Task PostStagingEndpoint_WithMultipartBody_Returns200WithStagingId()
    {
        var json = "{\"hello\":\"world\"}";

        using var content = new MultipartFormDataContent();
        var bodyPart = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        bodyPart.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(bodyPart, "body", "body.json");

        var response = await Client.PostAsync("system/request-staging", content);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        var payload = await response.ToResponseModel<StageBodyResponse>();
        payload.StagingId.Should().NotBeEmpty();
        _factory.RequestStaging.Exists(payload.StagingId).Should().BeTrue();
    }

    [Fact]
    public async Task PostStagingEndpoint_WithEmptyFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var bodyPart = new ByteArrayContent(Array.Empty<byte>());
        bodyPart.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(bodyPart, "body", "body.json");

        var response = await Client.PostAsync("system/request-staging", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Middleware_WithBogusStagingId_Returns400()
    {
        var bogus = Guid.NewGuid();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged");
        request.Headers.Add(RequestStagingConstants.BodyStagingIdHeader, bogus.ToString());
        request.Content = new StringContent(
            "{\"_stagingId\":\"" + bogus + "\"}",
            Encoding.UTF8,
            "application/json");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Request body staging entry not found");
    }

    [Fact]
    public async Task Middleware_WithoutHeader_PassesThrough()
    {
        // Sanity check: a normal POST without the staging header reaches the controller untouched.
        var response = await Client.PostAsJsonAsync(
            $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged",
            new ProcessStagedBulkUploadRequestDto(
                Guid.NewGuid(),
                new[] { ServiceName },
                Array.Empty<BulkUploadCellEditDto>()));

        // The controller returns 400 because the inner stagingId is unknown; the middleware was a no-op.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Upload session expired");
    }

    [Fact]
    public async Task Middleware_EndToEnd_RehydratesBodyForController()
    {
        // First: stage a CSV via the existing bulk-upload/validate path (this puts a CSV in bulk-upload-staging).
        var csv = BuildOutcomeScoresCsv(BuildDataRow(ServiceName, "GAD-7", "5", "6"));
        var validation = await PostValidateAsync(csv);
        validation.StagingId.Should().NotBeEmpty();

        // Build the JSON body the controller would have received if the frontend had POSTed directly.
        var originalRequest = new ProcessStagedBulkUploadRequestDto(
            validation.StagingId,
            new[] { ServiceName },
            Array.Empty<BulkUploadCellEditDto>());
        var originalJson = System.Text.Json.JsonSerializer.Serialize(originalRequest);

        // Stage that JSON body via /system/request-staging (the WAF-proven multipart path).
        var bodyStagingId = await StageJsonBodyAsync(originalJson);

        // POST a tiny stub to the controller with the X-Body-Staging-Id header.
        using var stubRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"organisation-admin/submissions/{_submissionId}/modules/{_moduleId}/bulk-upload/staged");
        stubRequest.Headers.Add(RequestStagingConstants.BodyStagingIdHeader, bodyStagingId.ToString());
        stubRequest.Content = new StringContent(
            "{\"_stagingId\":\"" + bodyStagingId + "\"}",
            Encoding.UTF8,
            "application/json");

        var response = await Client.SendAsync(stubRequest);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var result = await response.ToResponseModel<BulkUploadResultDto>();
        result.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);
        result.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task CleanupJob_RemovesExpiredButKeepsFresh()
    {
        var freshJson = "{\"fresh\":true}";
        using (var freshContent = new MultipartFormDataContent())
        {
            var part = new ByteArrayContent(Encoding.UTF8.GetBytes(freshJson));
            part.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            freshContent.Add(part, "body", "fresh.json");
            (await Client.PostAsync("system/request-staging", freshContent))
                .EnsureSuccessStatusCode();
        }

        var expiredId = Guid.NewGuid();
        _factory.RequestStaging.SeedExpired(
            expiredId,
            Encoding.UTF8.GetBytes("{\"expired\":true}"),
            "application/json",
            DateTime.UtcNow.AddHours(-48));

        var beforeCount = _factory.RequestStaging.Count;
        beforeCount.Should().BeGreaterThanOrEqualTo(2);

        using var scope = Server.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRequestStagingService>();
        var cleanup = await service.CleanupExpiredAsync(TimeSpan.FromHours(24));

        cleanup.Match(
            removed => removed.Should().Be(1),
            ex => throw ex);
        _factory.RequestStaging.Exists(expiredId).Should().BeFalse();
        _factory.RequestStaging.Count.Should().Be(beforeCount - 1);
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

        await SaveChangesAsync();

        var module = await Context.DataCollectionFormModules
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.OutcomeScores);
        _moduleId = module.Id.Value;
        _submissionId = Guid.NewGuid();
    }

    public async Task DisposeAsync()
    {
        _factory.RequestStaging.Clear();
        _factory.BlobService.Clear();
        await ClearAllTablesAsync();
    }

    private async Task<Guid> StageJsonBodyAsync(string json)
    {
        using var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        part.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(part, "body", "body.json");

        var response = await Client.PostAsync("system/request-staging", content);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var payload = await response.ToResponseModel<StageBodyResponse>();
        return payload.StagingId;
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

    private static string BuildOutcomeScoresCsv(params string[] rows)
    {
        var lines = new List<string> { string.Join(",", OutcomeScoresHeaders) };
        lines.AddRange(rows);
        return string.Join("\n", lines);
    }

    private static string BuildDataRow(string serviceName, string pps02, string pps08Pre, string pps08Post)
    {
        var cells = new Dictionary<string, string>
        {
            ["PPS01"] = serviceName,
            ["PPS02"] = pps02,
            ["PPS03"] = "white",
            ["PPS04"] = "male",
            ["PPS05"] = "5",
            ["PPS06_pre"] = string.Empty,
            ["PPS06_post"] = string.Empty,
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

    public sealed record StageBodyResponse(Guid StagingId, DateTime ExpiresAtUtc);
}