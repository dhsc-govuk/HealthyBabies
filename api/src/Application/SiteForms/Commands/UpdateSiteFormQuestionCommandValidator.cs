using FluentValidation;

namespace Application.SiteForms.Commands;

public class UpdateSiteFormQuestionCommandValidator : AbstractValidator<UpdateSiteFormQuestionCommand>
{
    public UpdateSiteFormQuestionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Hint)
            .MaximumLength(1000);

        RuleFor(x => x.Placeholder)
            .MaximumLength(255);

        RuleFor(x => x.QuestionType)
            .InclusiveBetween(0, 4);

        RuleFor(x => x.DisplayOrder)
            .GreaterThan(0);

        RuleFor(x => x.HelpTextSummary)
            .MaximumLength(500);

        RuleFor(x => x.ConditionalQuestionCode)
            .MaximumLength(50);

        RuleFor(x => x.ConditionalValue)
            .MaximumLength(255);
    }
}