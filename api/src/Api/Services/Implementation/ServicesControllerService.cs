using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Services.Dtos;
using Domain.ServiceForms;
using Domain.Services;
using LanguageExt;

namespace Api.Services.Implementation;

public class ServicesControllerService(
    IServiceQueries serviceQueries,
    IServiceFormQuestionQueries questionQueries) : IServicesControllerService
{
    public async Task<IEnumerable<ServiceListDto>> GetAll(
        Permission permission,
        CancellationToken cancellationToken)
    {
        var services = await serviceQueries.GetAll(permission, cancellationToken);
        return services.Select(ServiceListDto.FromDomainModel);
    }

    public async Task<Option<ServiceDto>> Get(
        Permission permission,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var service = await serviceQueries.GetById(permission, new ServiceId(serviceId), cancellationToken);
        return service.Match(
            s => ServiceDto.FromDomainModel(s),
            () => Option<ServiceDto>.None);
    }

    public async Task<string> GenerateBulkUploadTemplate(CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAllActive(cancellationToken);
        var orderedQuestions = questions
            .OrderBy(q => q.Step)
            .ThenBy(q => q.DisplayOrder)
            .ToList();

        var sb = new StringBuilder();

        // Row 1: Question codes
        var codes = orderedQuestions.Select(q => EscapeCsvField(q.Code));
        sb.AppendLine(string.Join(",", codes));

        // Row 2: Question labels
        var labels = orderedQuestions.Select(q => EscapeCsvField(q.Label));
        sb.AppendLine(string.Join(",", labels));

        // Row 3: Example row with hints/placeholders
        var hints = orderedQuestions.Select(q => GetExampleValue(q));
        sb.AppendLine(string.Join(",", hints));

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static string GetExampleValue(ServiceFormQuestion question)
    {
        if (question.Options.Any())
        {
            if (question.QuestionType == ServiceFormQuestionType.Checkbox)
            {
                // For checkbox, show comma-separated labels: "Targeted,Specialist"
                var firstTwo = question.Options.Take(2).Select(o => o.Label);
                return EscapeCsvField(string.Join(",", firstTwo));
            }

            // For radio/select, show first option label
            return EscapeCsvField(question.Options.First().Label);
        }

        if (question.QuestionType == ServiceFormQuestionType.Date)
        {
            return "2024-01-01";
        }

        // For text, use placeholder or hint
        return EscapeCsvField(question.Placeholder ?? question.Hint ?? "Example text");
    }
}