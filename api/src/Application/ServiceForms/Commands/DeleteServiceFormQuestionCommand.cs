using Application.Common.Interfaces.Repositories;
using Application.ServiceForms.Exceptions;
using Domain.ServiceForms;
using LanguageExt;
using MediatR;

namespace Application.ServiceForms.Commands;

public record DeleteServiceFormQuestionCommand : IRequest<Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public Guid Id { get; init; }
}

public class DeleteServiceFormQuestionCommandHandler(IServiceFormQuestionRepository repository)
    : IRequestHandler<DeleteServiceFormQuestionCommand, Either<ServiceFormQuestionException, ServiceFormQuestion>>
{
    public async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> Handle(
        DeleteServiceFormQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var questionId = ServiceFormQuestionId.From(request.Id);
        var question = await repository.GetById(questionId, cancellationToken);

        return await question.MatchAsync(
            q => DeleteQuestion(q, cancellationToken),
            () => new ServiceFormQuestionNotFoundException(questionId));
    }

    private async Task<Either<ServiceFormQuestionException, ServiceFormQuestion>> DeleteQuestion(
        ServiceFormQuestion question,
        CancellationToken cancellationToken)
    {
        if (question.IsPredefined)
        {
            return new ServiceFormQuestionCannotDeletePredefinedException(question.Id, question.Code);
        }

        try
        {
            await repository.DeleteAsync(question, cancellationToken);
            return question;
        }
        catch (Exception exception)
        {
            return new ServiceFormQuestionUnknownException(question.Id, exception);
        }
    }
}