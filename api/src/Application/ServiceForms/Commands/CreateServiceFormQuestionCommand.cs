using Application.Common.Interfaces.Repositories;
using Application.ServiceForms.Dtos;
using Application.ServiceForms.Exceptions;
using Domain.ServiceForms;
using LanguageExt;
using MediatR;

namespace Application.ServiceForms.Commands;

public record CreateServiceFormQuestionCommand : IRequest<Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public int QuestionType { get; init; }
    public int Step { get; init; }
    public bool IsRequired { get; init; }
    public bool IsPredefined { get; init; } = true;
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public IReadOnlyList<ServiceFormQuestionOptionInputDto> Options { get; init; } = [];
}

public class CreateServiceFormQuestionCommandHandler(IServiceFormQuestionRepository repository)
    : IRequestHandler<CreateServiceFormQuestionCommand, Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> Handle(
        CreateServiceFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        return await ValidateDuplicateCodeAsync(request, cancellationToken)
            .BindAsync(validatedRequest => CreateQuestionAsync(validatedRequest, cancellationToken));
    }

    private async Task<Either<ServiceFormQuestionException, CreateServiceFormQuestionCommand>> ValidateDuplicateCodeAsync(
        CreateServiceFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.FindByCodeAsync(request.Code, cancellationToken);
        return existing.IsSome
            ? new ServiceFormQuestionDuplicateCodeException(request.Code)
            : request;
    }

    private async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> CreateQuestionAsync(
        CreateServiceFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var maxDisplayOrder = await repository.GetMaxDisplayOrderForStep(request.Step, cancellationToken);
            var displayOrder = maxDisplayOrder + 1;

            var question = ServiceFormQuestion.New(
                ServiceFormQuestionId.New(),
                request.Code,
                request.Label,
                request.Hint,
                request.Placeholder,
                (ServiceFormQuestionType)request.QuestionType,
                request.Step,
                displayOrder,
                request.IsRequired,
                request.IsPredefined,
                request.HelpTextSummary,
                request.HelpText,
                request.ConditionalQuestionCode,
                request.ConditionalValue);

            for (var i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                question.AddOption(option.Value, option.Label, option.DisplayOrder);
            }

            return await repository.AddAsync(question, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceFormQuestionUnknownException(ServiceFormQuestionId.Empty(), exception);
        }
    }
}