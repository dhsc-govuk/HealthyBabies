using FluentValidation;

namespace Application.SiteForms.Commands;

public class CreateSiteFormQuestionCommandValidator : AbstractValidator<CreateSiteFormQuestionCommand>
{
    public CreateSiteFormQuestionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Hint)
            .MaximumLength(1000);

        RuleFor(x => x.Placeholder)
            .MaximumLength(255);

        RuleFor(x => x.QuestionType)
            .InclusiveBetween(0, 4);

        RuleFor(x => x.HelpTextSummary)
            .MaximumLength(500);

        RuleFor(x => x.ConditionalQuestionCode)
            .MaximumLength(50);

        RuleFor(x => x.ConditionalValue)
            .MaximumLength(255);
    }
}