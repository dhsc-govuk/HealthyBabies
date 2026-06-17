using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceForms.Dtos;
using Application.Services.Exceptions;
using Domain.ServiceForms;
using Domain.Services;
using FluentValidation;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;

namespace Application.Services.Commands;

public record BulkUpdateServicesCommand : IRequest<Either<ServiceException, BulkUpdateServicesResult>>
{
    public IReadOnlyList<ServiceBulkUpdateItem> Services { get; init; } = [];
}

public record ServiceBulkUpdateItem(
    Guid? ServiceId,
    string Name,
    IReadOnlyList<ServiceAnswerInputDto> Answers);

public record BulkUpdateServicesResult(
    int TotalCount,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<BulkUpdateServiceResult> Results);

public record BulkUpdateServiceResult(
    Guid? ServiceId,
    string ServiceName,
    bool IsSuccess,
    bool IsNew,
    string? ErrorMessage = null);

public class BulkUpdateServicesCommandValidator : AbstractValidator<BulkUpdateServicesCommand>
{
    public BulkUpdateServicesCommandValidator()
    {
        RuleFor(x => x.Services)
            .NotEmpty()
            .WithMessage("At least one service must be provided for bulk update");

        RuleForEach(x => x.Services)
            .ChildRules(service =>
            {
                service.RuleFor(s => s.Name)
                    .NotEmpty()
                    .WithMessage("Service name is required");

                service.RuleFor(s => s.Answers)
                    .NotEmpty()
                    .WithMessage("At least one answer is required");
            });
    }
}

public class BulkUpdateServicesCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository,
    IServiceFormQuestionQueries questionQueries)
    : IRequestHandler<BulkUpdateServicesCommand, Either<ServiceException, BulkUpdateServicesResult>>
{
    public async Task<Either<ServiceException, BulkUpdateServicesResult>> Handle(
        BulkUpdateServicesCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await ProcessBulkUpdate(p, request, cancellationToken),
            e => new ServiceArgumentException(ServiceId.Empty(), e.Message));
    }

    private async Task<Either<ServiceException, BulkUpdateServicesResult>> ProcessBulkUpdate(
        Permission permission,
        BulkUpdateServicesCommand request,
        CancellationToken cancellationToken)
    {
        var results = new List<BulkUpdateServiceResult>();
        var allQuestions = await questionQueries.GetAllActive(cancellationToken);

        foreach (var item in request.Services)
        {
            var result = await ProcessSingleService(permission, item, allQuestions, cancellationToken);
            results.Add(result);
        }

        var successCount = results.Count(r => r.IsSuccess);
        var errorCount = results.Count(r => !r.IsSuccess);

        return new BulkUpdateServicesResult(
            results.Count,
            successCount,
            errorCount,
            results);
    }

    private async Task<BulkUpdateServiceResult> ProcessSingleService(
        Permission permission,
        ServiceBulkUpdateItem item,
        IReadOnlyList<ServiceFormQuestion> allQuestions,
        CancellationToken cancellationToken)
    {
        try
        {
            if (item.ServiceId.HasValue)
            {
                return await UpdateExistingService(permission, item, allQuestions, cancellationToken);
            }
            else
            {
                return await CreateNewService(permission, item, allQuestions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return new BulkUpdateServiceResult(
                item.ServiceId,
                item.Name,
                false,
                false,
                ex.Message);
        }
    }

    private async Task<BulkUpdateServiceResult> UpdateExistingService(
        Permission permission,
        ServiceBulkUpdateItem item,
        IReadOnlyList<ServiceFormQuestion> allQuestions,
        CancellationToken cancellationToken)
    {
        var serviceId = new ServiceId(item.ServiceId!.Value);
        var serviceOption = await serviceRepository.GetByIdForUpdate(permission, serviceId, cancellationToken);

        if (serviceOption.IsNone)
        {
            return new BulkUpdateServiceResult(
                item.ServiceId,
                item.Name,
                false,
                false,
                "Service not found or access denied");
        }

        var service = serviceOption.ValueUnsafe();

        // Update service name from SMD01 if provided
        var nameAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == "SMD01");
        if (nameAnswer?.Value != null)
        {
            service.UpdateName(nameAnswer.Value);
        }

        // Clear all existing answers and add new ones
        service.ClearAnswersForStep(1);
        service.ClearAnswersForStep(2);

        // Process each answer
        foreach (var answerInput in item.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
            if (question == null)
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, answerInput.Value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            service.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.Step,
                question.DisplayOrder,
                answerInput.Value,
                displayValue,
                optionsSnapshot);
        }

        // Mark service as complete
        service.AdvanceToStep(3);
        service.Complete();

        await serviceRepository.UpdateAsync(service, cancellationToken);

        return new BulkUpdateServiceResult(
            item.ServiceId,
            item.Name,
            true,
            false);
    }

    private async Task<BulkUpdateServiceResult> CreateNewService(
        Permission permission,
        ServiceBulkUpdateItem item,
        IReadOnlyList<ServiceFormQuestion> allQuestions,
        CancellationToken cancellationToken)
    {
        var nameAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == "SMD01");
        var name = nameAnswer?.Value ?? item.Name;

        var organisationId = permission.OrganisationId.IfNone(() =>
            throw new InvalidOperationException("Organisation ID is required to create a service"));

        var service = Service.New(ServiceId.New(), organisationId, name);

        // Process each answer
        foreach (var answerInput in item.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
            if (question == null)
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, answerInput.Value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            service.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.Step,
                question.DisplayOrder,
                answerInput.Value,
                displayValue,
                optionsSnapshot);
        }

        // Mark service as complete
        service.AdvanceToStep(3);
        service.Complete();

        await serviceRepository.AddAsync(service, cancellationToken);

        return new BulkUpdateServiceResult(
            service.Id.Value,
            name,
            true,
            true);
    }

    private static string? GetDisplayValue(ServiceFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == ServiceFormQuestionType.Checkbox)
        {
            var values = value.Split(',');
            var labels = values
                .Select(v => question.Options.FirstOrDefault(o => o.Value == v)?.Label ?? v)
                .ToList();
            return string.Join(", ", labels);
        }

        if (question.Options.Any())
        {
            var option = question.Options.FirstOrDefault(o => o.Value == value);
            return option?.Label ?? value;
        }

        return value;
    }
}