using Application.Common.Interfaces.Repositories;
using Application.SiteForms.Dtos;
using Application.SiteForms.Exceptions;
using Domain.SiteForms;
using LanguageExt;
using MediatR;

namespace Application.SiteForms.Commands;

public record ReorderSiteFormQuestionsCommand : IRequest<Either<SiteFormQuestionException, LanguageExt.Unit>>
{
    public IReadOnlyList<SiteFormQuestionOrderDto> Questions { get; init; } = [];
}

public class ReorderSiteFormQuestionsCommandHandler(ISiteFormQuestionRepository repository)
    : IRequestHandler<ReorderSiteFormQuestionsCommand, Either<SiteFormQuestionException, LanguageExt.Unit>>
{
    public async Task<Either<SiteFormQuestionException, LanguageExt.Unit>> Handle(
        ReorderSiteFormQuestionsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var questions = await repository.GetAllTracking(cancellationToken);
            var questionsDict = questions.ToDictionary(q => q.Id.Value);

            foreach (var orderDto in request.Questions)
            {
                if (questionsDict.TryGetValue(orderDto.Id, out var question))
                {
                    question.SetDisplayOrder(orderDto.DisplayOrder);
                }
            }

            await repository.SaveChangesAsync(cancellationToken);
            return LanguageExt.Unit.Default;
        }
        catch (Exception exception)
        {
            return new SiteFormQuestionUnknownException(SiteFormQuestionId.Empty(), exception);
        }
    }
}