using FluentValidation;

namespace Application.SiteForms.Commands;

public class DeleteSiteFormQuestionCommandValidator : AbstractValidator<DeleteSiteFormQuestionCommand>
{
    public DeleteSiteFormQuestionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}