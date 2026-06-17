using Application.Common.Interfaces.Repositories;
using Application.SiteForms.Dtos;
using Application.SiteForms.Exceptions;
using Domain.SiteForms;
using LanguageExt;
using MediatR;

namespace Application.SiteForms.Commands;

public record UpdateSiteFormQuestionCommand : IRequest<Either<SiteFormQuestionException, SiteFormQuestion>>
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public int QuestionType { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public IReadOnlyList<SiteFormQuestionOptionInputDto> Options { get; init; } = [];
}

public class UpdateSiteFormQuestionCommandHandler(ISiteFormQuestionRepository repository)
    : IRequestHandler<UpdateSiteFormQuestionCommand, Either<SiteFormQuestionException, SiteFormQuestion>>
{
    public async Task<Either<SiteFormQuestionException, SiteFormQuestion>> Handle(
        UpdateSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var questionId = new SiteFormQuestionId(request.Id);
        var questionResult = await repository.GetById(questionId, cancellationToken);

        return await questionResult.MatchAsync(
            question => UpdateQuestionAsync(question, request, cancellationToken),
            () => Task.FromResult<Either<SiteFormQuestionException, SiteFormQuestion>>(
                new SiteFormQuestionNotFoundException(questionId)));
    }

    private async Task<Either<SiteFormQuestionException, SiteFormQuestion>> UpdateQuestionAsync(
        SiteFormQuestion question,
        UpdateSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            question.UpdateDetails(
                request.Label,
                request.Hint,
                request.Placeholder,
                (SiteFormQuestionType)request.QuestionType,
                request.DisplayOrder,
                request.IsRequired,
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
            return new SiteFormQuestionUnknownException(question.Id, exception);
        }
    }
}