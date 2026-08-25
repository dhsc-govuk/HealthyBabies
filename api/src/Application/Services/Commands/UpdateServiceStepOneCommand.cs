using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceForms.Dtos;
using Application.Services.Exceptions;
using Application.Services.Helpers;
using Domain.ServiceForms;
using Domain.Services;
using LanguageExt;
using MediatR;

namespace Application.Services.Commands;

public record UpdateServiceStepOneCommand : IRequest<Either<ServiceException, Service>>
{
    public Guid ServiceId { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<ServiceAnswerInputDto> Answers { get; init; } = [];
    public bool AdvanceStep { get; init; } = true;
}

public class UpdateServiceStepOneCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository,
    IServiceFormQuestionQueries questionQueries)
    : IRequestHandler<UpdateServiceStepOneCommand, Either<ServiceException, Service>>
{
    public async Task<Either<ServiceException, Service>> Handle(
        UpdateServiceStepOneCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceId = new ServiceId(request.ServiceId);
                var serviceResult = await GetService(serviceId, p, cancellationToken);

                return await serviceResult.BindAsync<ServiceException, Service, Service>(service =>
                    FindDuplicate(service, request, cancellationToken));
            },
            e => new ServiceArgumentException(new ServiceId(request.ServiceId), e.Message));
    }

    private async Task<Either<ServiceException, Service>> FindDuplicate(
        Service service,
        UpdateServiceStepOneCommand request,
        CancellationToken cancellationToken)
    {
        var duplicate = await serviceRepository.FindDuplicateAsync(
            request.Name!,
            service.OrganisationId,
            service.Id,
            cancellationToken);

        return await duplicate.MatchAsync(
            s => new ServiceAlreadyExistsException(s.Id, request.Name!),
            () => UpdateService(service, request, cancellationToken));
    }

    private async Task<Either<ServiceException, Service>> UpdateService(
        Service service,
        UpdateServiceStepOneCommand request,
        CancellationToken cancellationToken)
    {
        // Get step 1 questions
        var questions = await questionQueries.GetByStep(1, cancellationToken);

        var dateError = ServiceFormDateAnswerValidator.ValidateAnswers(request.Answers, questions);
        if (dateError != null)
        {
            return new ServiceArgumentException(service.Id, dateError);
        }

        try
        {
            // Revert to draft if the service was complete (user is editing a completed service)
            service.RevertToDraft();

            // Update service name
            service.UpdateName(request.Name!);

            // Process each answer
            foreach (var answerInput in request.Answers)
            {
                var question = questions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
                if (question == null)
                {
                    continue;
                }

                var displayValue = GetDisplayValue(question, answerInput.Value);
                var optionsSnapshot = question.Options.Any()
                    ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                    : null;

                var existingAnswer = service.GetAnswer(answerInput.QuestionCode);
                if (existingAnswer != null)
                {
                    existingAnswer.UpdateAnswer(answerInput.Value, displayValue);
                    existingAnswer.UpdateSnapshot(
                        question.Label,
                        question.Hint,
                        question.QuestionType,
                        question.DisplayOrder,
                        optionsSnapshot);
                }
                else
                {
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
            }

            RemoveOrphanedAnswers(service, request.Answers, questions);

            if (request.AdvanceStep)
            {
                // SMD04 (closing date) is only submitted when the service is no longer offered.
                // Its presence signals that service characteristics (step 2) are not applicable.
                var closingDateAnswer = request.Answers.FirstOrDefault(a => a.QuestionCode == "SMD04");
                if (!string.IsNullOrEmpty(closingDateAnswer?.Value))
                {
                    service.ClearAnswersForStep(2);
                    service.AdvanceToStep(3);
                }
                else
                {
                    service.AdvanceToStep(2);
                }
            }

            return await serviceRepository.UpdateAsync(service, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceUnknownException(service.Id, exception);
        }
    }

    // See the note on the step two equivalent. SMD01 is excluded because the client never sends it -
    // the name is handled separately by UpdateName - but bulk upload does write an SMD01 answer, and
    // the core data export builds its SMD01 column from exactly that row. Without this, the first UI
    // edit of a bulk uploaded service would silently blank it.
    private static void RemoveOrphanedAnswers(
        Service service,
        IReadOnlyList<ServiceAnswerInputDto> answers,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var submittedCodes = answers.Select(a => a.QuestionCode).ToHashSet();
        var orphanedCodes = questions
            .Select(q => q.Code)
            .Where(code => code != ServiceBulkUploadRecordHelper.PredefinedNameCode && !submittedCodes.Contains(code))
            .ToList();

        service.RemoveAnswers(orphanedCodes);
    }

    private static string? GetDisplayValue(ServiceFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == ServiceFormQuestionType.Checkbox)
        {
            // For checkboxes, value is comma-separated
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

    private async Task<Either<ServiceException, Service>> GetService(
        ServiceId serviceId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdForUpdate(permission, serviceId, cancellationToken);
        return service.Match<Either<ServiceException, Service>>(
            s => s,
            () => new ServiceDoesNotExistException(serviceId));
    }
}