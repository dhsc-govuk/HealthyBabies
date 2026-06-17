using FluentValidation;

namespace Application.ServiceForms.Commands;

public class DeleteServiceFormQuestionCommandValidator : AbstractValidator<DeleteServiceFormQuestionCommand>
{
    public DeleteServiceFormQuestionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}