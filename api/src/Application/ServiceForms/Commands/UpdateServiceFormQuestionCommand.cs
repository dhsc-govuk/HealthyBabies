using Application.Common.Interfaces.Repositories;
using Application.ServiceForms.Dtos;
using Application.ServiceForms.Exceptions;
using Domain.ServiceForms;
using LanguageExt;
using MediatR;

namespace Application.ServiceForms.Commands;

public record UpdateServiceFormQuestionCommand : IRequest<Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public int QuestionType { get; init; }
    public int Step { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsPredefined { get; init; } = true;
    public bool IsActive { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public IReadOnlyList<ServiceFormQuestionOptionInputDto> Options { get; init; } = [];
}

public class UpdateServiceFormQuestionCommandHandler(IServiceFormQuestionRepository repository)
    : IRequestHandler<UpdateServiceFormQuestionCommand, Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> Handle(
        UpdateServiceFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var questionId = ServiceFormQuestionId.From(request.Id);
        var question = await repository.GetById(questionId, cancellationToken);

        return await question.MatchAsync(
            q => UpdateQuestion(request, q, cancellationToken),
            () => new ServiceFormQuestionNotFoundException(questionId));
    }

    private async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> UpdateQuestion(
        UpdateServiceFormQuestionCommand request,
        ServiceFormQuestion question,
        CancellationToken cancellationToken)
    {
        try
        {
            question.UpdateDetails(
                request.Label,
                request.Hint,
                request.Placeholder,
                (ServiceFormQuestionType)request.QuestionType,
                request.Step,
                request.DisplayOrder,
                request.IsRequired,
                request.IsPredefined,
                request.HelpTextSummary,
                request.HelpText,
                request.ConditionalQuestionCode,
                request.ConditionalValue);

            if (request.IsActive)
            {
                question.Activate();
            }
            else
            {
                question.Deactivate();
            }

            question.ClearOptions();
            foreach (var option in request.Options)
            {
                question.AddOption(option.Value, option.Label, option.DisplayOrder);
            }

            return await repository.UpdateAsync(question, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ServiceFormQuestionUnknownException(question.Id, exception);
        }
    }
}