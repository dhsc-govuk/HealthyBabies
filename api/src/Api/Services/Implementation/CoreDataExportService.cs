using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;

namespace Api.Services.Implementation;

public class CoreDataExportService(
    ILocationQueries locationQueries,
    IServiceQueries serviceQueries,
    ISiteFormQuestionQueries siteFormQuestionQueries,
    IServiceFormQuestionQueries serviceFormQuestionQueries)
    : ICoreDataExportService
{
    public async Task<byte[]> ExportSitesAsCsvAsync(CancellationToken cancellationToken = default)
    {
        var questions = await siteFormQuestionQueries.GetAllActive(cancellationToken);
        var locations = await locationQueries.GetAllForExport(cancellationToken);

        var sb = new StringBuilder();

        // Header row: LAName + static location fields + question codes
        var headers = new List<string> { "LAName", "SiteName", "ReferenceNumber", "PostCode", "AddressLine1", "AddressLine2", "TownOrCity", "County", "IsActive" };
        headers.AddRange(questions.Select(q => q.Code));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        // Label row: blank for static columns, question labels for answer columns
        var labels = new List<string> { "LA Name", "Site Name", "Reference Number", "Post Code", "Address Line 1", "Address Line 2", "Town or City", "County", "Is Active" };
        labels.AddRange(questions.Select(q => q.Label));
        sb.AppendLine(string.Join(",", labels.Select(EscapeCsvField)));

        foreach (var location in locations)
        {
            var answerLookup = location.Answers.ToDictionary(a => a.QuestionCode, a => a.DisplayValue ?? a.Value ?? string.Empty);
            var row = new List<string>
            {
                location.Organisation?.Name ?? string.Empty,
                location.Name,
                location.ReferenceNumber,
                location.PostCode,
                location.AddressLine1,
                location.AddressLine2,
                location.TownOrCity,
                location.County,
                location.IsActive ? "Yes" : "No",
            };
            row.AddRange(questions.Select(q => answerLookup.TryGetValue(q.Code, out var val) ? val : string.Empty));
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportServicesAsCsvAsync(CancellationToken cancellationToken = default)
    {
        var questions = await serviceFormQuestionQueries.GetAllActive(cancellationToken);
        var services = await serviceQueries.GetAllForExport(cancellationToken);

        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ServiceName", "UniqueReference", "Status" };
        headers.AddRange(questions.Select(q => q.Code));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var labels = new List<string> { "LA Name", "Service Name", "Unique Reference", "Status" };
        labels.AddRange(questions.Select(q => q.Label));
        sb.AppendLine(string.Join(",", labels.Select(EscapeCsvField)));

        foreach (var service in services)
        {
            var answerLookup = service.Answers.ToDictionary(a => a.QuestionCode, a => a.DisplayValue ?? a.Value ?? string.Empty);
            var row = new List<string>
            {
                service.Organisation?.Name ?? string.Empty,
                service.Name,
                service.Id.Value.ToString(),
                service.Status.ToString(),
            };
            row.AddRange(questions.Select(q => answerLookup.TryGetValue(q.Code, out var val) ? val : string.Empty));
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return field;
        }

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}