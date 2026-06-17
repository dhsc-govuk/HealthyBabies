using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.ServiceCategories.Exceptions;
using Application.ServiceCategoryForms.Dtos;
using Domain.ServiceCategories;
using Domain.ServiceCategoryForms;
using LanguageExt;
using MediatR;

namespace Application.ServiceCategories.Commands;

public record UpdateServiceCategoryStepOneCommand : IRequest<Either<ServiceCategoryException, ServiceCategory>>
{
    public Guid ServiceCategoryId { get; init; }
    public IReadOnlyList<ServiceCategoryAnswerInputDto> Answers { get; init; } = [];
    public bool AdvanceStep { get; init; } = true;
}

public class UpdateServiceCategoryStepOneCommandHandler(
    PermissionsService permissionsService,
    IServiceCategoryRepository serviceCategoryRepository,
    IServiceCategoryFormQuestionQueries questionQueries)
    : IRequestHandler<UpdateServiceCategoryStepOneCommand, Either<ServiceCategoryException, ServiceCategory>>
{
    public async Task<Either<ServiceCategoryException, ServiceCategory>> Handle(
        UpdateServiceCategoryStepOneCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var serviceCategoryId = new ServiceCategoryId(request.ServiceCategoryId);
                var serviceCategoryResult = await GetServiceCategory(serviceCategoryId, p, cancellationToken);

                return await serviceCategoryResult.BindAsync<ServiceCategoryException, ServiceCategory, ServiceCategory>(
                    serviceCategory => UpdateServiceCategory(serviceCategory, request, cancellationToken));
            },
            e => new ServiceCategoryArgumentException(new ServiceCategoryId(request.ServiceCategoryId), e.Message));
    }

    private async Task<Either<ServiceCategoryException, ServiceCategory>> UpdateServiceCategory(
        ServiceCategory serviceCategory,
        UpdateServiceCategoryStepOneCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            serviceCategory.RevertToDraft();

            var questions = await questionQueries.GetByStep(1, cancellationToken);

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

                var existingAnswer = serviceCategory.GetAnswer(answerInput.QuestionCode);
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
                    serviceCategory.AddAnswer(
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

            if (request.AdvanceStep)
            {
                serviceCategory.AdvanceToStep(2);
            }

            return await serviceCategoryRepository.UpdateAsync(serviceCategory, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceCategoryUnknownException(serviceCategory.Id, exception);
        }
    }

    private static string? GetDisplayValue(ServiceCategoryFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == ServiceCategoryFormQuestionType.Checkbox)
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

    private async Task<Either<ServiceCategoryException, ServiceCategory>> GetServiceCategory(
        ServiceCategoryId serviceCategoryId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var serviceCategory = await serviceCategoryRepository.GetByIdForUpdate(permission, serviceCategoryId, cancellationToken);
        return serviceCategory.Match<Either<ServiceCategoryException, ServiceCategory>>(
            s => s,
            () => new ServiceCategoryDoesNotExistException(serviceCategoryId));
    }
}