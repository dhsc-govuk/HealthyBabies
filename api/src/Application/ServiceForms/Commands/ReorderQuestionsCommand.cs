using Application.Common.Interfaces.Repositories;
using Application.ServiceForms.Dtos;
using Application.ServiceForms.Exceptions;
using Domain.ServiceForms;
using LanguageExt;
using MediatR;

namespace Application.ServiceForms.Commands;

public record ReorderQuestionsCommand : IRequest<Either<ServiceFormQuestionException, LanguageExt.Unit>>
{
    public int Step { get; init; }
    public IReadOnlyList<QuestionOrderDto> Questions { get; init; } = [];
}

public class ReorderQuestionsCommandHandler(IServiceFormQuestionRepository repository)
    : IRequestHandler<ReorderQuestionsCommand, Either<ServiceFormQuestionException, LanguageExt.Unit>>
{
    public async Task<Either<ServiceFormQuestionException, LanguageExt.Unit>> Handle(
        ReorderQuestionsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var questions = await repository.GetByStepTracking(request.Step, cancellationToken);
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
            return new ServiceFormQuestionUnknownException(ServiceFormQuestionId.Empty(), exception);
        }
    }
}