using Application.Common.Interfaces.Repositories;
using Application.SiteForms.Exceptions;
using Domain.SiteForms;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.SiteForms.Commands;

public record DeleteSiteFormQuestionCommand : IRequest<Either<SiteFormQuestionException, Unit>>
{
    public Guid Id { get; init; }
}

public class DeleteSiteFormQuestionCommandHandler(ISiteFormQuestionRepository repository)
    : IRequestHandler<DeleteSiteFormQuestionCommand, Either<SiteFormQuestionException, Unit>>
{
    public async Task<Either<SiteFormQuestionException, Unit>> Handle(
        DeleteSiteFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var questionId = new SiteFormQuestionId(request.Id);
        var questionResult = await repository.GetById(questionId, cancellationToken);

        return await questionResult.MatchAsync(
            question => DeleteQuestionAsync(question, cancellationToken),
            () => Task.FromResult<Either<SiteFormQuestionException, Unit>>(
                new SiteFormQuestionNotFoundException(questionId)));
    }

    private async Task<Either<SiteFormQuestionException, Unit>> DeleteQuestionAsync(
        SiteFormQuestion question,
        CancellationToken cancellationToken)
    {
        if (question.IsPredefined)
        {
            return new SiteFormQuestionCannotDeletePredefinedException(question.Id, question.Code);
        }

        try
        {
            await repository.DeleteAsync(question, cancellationToken);
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return new SiteFormQuestionUnknownException(question.Id, exception);
        }
    }
}