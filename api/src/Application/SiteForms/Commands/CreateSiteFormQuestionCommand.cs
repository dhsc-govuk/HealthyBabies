using Application.Common.Interfaces.Repositories;
using Application.SiteForms.Dtos;
using Application.SiteForms.Exceptions;
using Domain.SiteForms;
using LanguageExt;
using MediatR;

namespace Application.SiteForms.Commands;

public record CreateSiteFormQuestionCommand : IRequest<Either<SiteFormQuestionException, SiteFormQuestion>>
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public int QuestionType { get; init; }
    public bool IsRequired { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public IReadOnlyList<SiteFormQuestionOptionInputDto> Options { get; init; } = [];
}

public class CreateSiteFormQuestionCommandHandler(ISiteFormQuestionRepository repository)
    : IRequestHandler<CreateSiteFormQuestionCommand, Either<SiteFormQuestionException, SiteFormQuestion>>
{
    public async Task<Either<SiteFormQuestionException, SiteFormQuestion>> Handle(
        CreateSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        return await ValidateDuplicateCodeAsync(request, cancellationToken)
            .BindAsync(validatedRequest => CreateQuestionAsync(validatedRequest, cancellationToken));
    }

    private async Task<Either<SiteFormQuestionException, CreateSiteFormQuestionCommand>> ValidateDuplicateCodeAsync(
        CreateSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.FindByCodeAsync(request.Code, cancellationToken);
        return existing.IsSome
            ? new SiteFormQuestionDuplicateCodeException(request.Code)
            : request;
    }

    private async Task<Either<SiteFormQuestionException, SiteFormQuestion>> CreateQuestionAsync(
        CreateSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var maxDisplayOrder = await repository.GetMaxDisplayOrder(cancellationToken);
            var displayOrder = maxDisplayOrder + 1;

            var question = SiteFormQuestion.New(
                SiteFormQuestionId.New(),
                request.Code,
                request.Label,
                request.Hint,
                request.Placeholder,
                (SiteFormQuestionType)request.QuestionType,
                displayOrder,
                request.IsRequired,
                isPredefined: false,
                request.HelpTextSummary,
                request.HelpText,
                request.ConditionalQuestionCode,
                request.ConditionalValue);

            foreach (var option in request.Options)
            {
                question.AddOption(option.Value, option.Label, option.DisplayOrder);
            }

            return await repository.AddAsync(question, cancellationToken);
        }
        catch (Exception exception)
        {
            return new SiteFormQuestionUnknownException(SiteFormQuestionId.Empty(), exception);
        }
    }
}